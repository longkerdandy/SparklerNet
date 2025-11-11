# Message Ordering Service

## Overview

The `MessageOrderingService` is a critical part of the Sparkplug Host Application that ensures messages from Edge
Nodes are processed in the correct sequence according to the Sparkplug specification. This service handles the
validation of sequence numbers, caching of out-of-order messages, and implementation of reordering timeouts as required
by the Sparkplug standard.

## Design Principles

This implementation follows the requirements specified in the Sparkplug specification regarding message ordering:

- Validates sequence numbers (0~255 range) sent in NBIRTH, DBIRTH, NDATA, and DDATA messages
- Handles out-of-order message arrival, which can occur when using clustered MQTT servers
- Implements configurable reordering timeout for missing messages
- Supports Rebirth Request sending when gaps persist beyond the timeout
- Provides thread-safe operations for concurrent message processing
- Handles circular sequence number wrap-around (0~255)

## Code Architecture

### Core Components

- **IMessageOrderingService Interface**: Defines the contract for message ordering operations
- **MessageOrderingService Class**: Implements the message ordering logic
- **CircularSequenceComparer Class**: Custom comparer for proper circular sequence ordering
- **Delegates**: `RebirthRequestCallback` and `PendingMessagesCallback` for event notification

## Message Processing Flow

### Reorder Processing Flow

```
ProcessMessageOrder(SparkplugMessageEventArgs message)
├── Validate message is not null
├── Validate sequence number is within range (0-255)
├── Acquire fine-grained lock for thread safety
│   ├── Build cache key for sequence tracking
│   ├── Check if previous sequence exists in cache
│   │   ├── YES ─┬─ Calculate expected next sequence (with wrap-around)
│   │   │        └─ Compare with current sequence
│   │   │           ├── MATCH ── Update cache with current sequence
│   │   │           │           ├─ Add current message to results
│   │   │           │           └─ Process any now-continuous pending messages
│   │   │           │               ├── Build all required cache keys (seqKey, pendingKey, timerKey)
│   │   │           │               ├── Check if pending messages exist
│   │   │           │               ├── Process pending messages in sequential order
│   │   │           │               │   ├── Determine current sequence and expected next sequence
│   │   │           │               │   ├── Loop until no more continuous messages found
│   │   │           │               │   ├── Check if first pending message matches expected sequence
│   │   │           │               │   ├── If matches, add to results and remove from pending collection
│   │   │           │               │   └── Update current sequence and expected next sequence
│   │   │           │               ├── Update cache with new current sequence
│   │   │           │               └── Update or clean up cache and timer
│   │   │           │                   ├── If pending messages remain ── Update cache and reset timer
│   │   │           │                   └── If no pending messages ── Remove cache entry and dispose timer
│   │   │           │
│   │   │           └─ NO MATCH ── Cache the message for later processing
│   │   │               ├── Build cache key for pending messages
│   │   │               ├── Get or create sorted collection with CircularSequenceComparer
│   │   │               ├── Add message to pending collection
│   │   │               ├── Update pending messages cache
│   │   │               └── Set up reorder timeout timer
│   │   │
│   │   └── NO previous sequence ── Update cache with current sequence
│   │                           ├─ Add current message to results
│   │                           └─ Process any now-continuous pending messages
│   │                               ├── Build all required cache keys (seqKey, pendingKey, timerKey)
│   │                               ├── Check if pending messages exist
│   │                               ├── Process pending messages in sequential order
│   │                               │   ├── Determine current sequence and expected next sequence
│   │                               │   ├── Loop until no more continuous messages found
│   │                               │   ├── Check if first pending message matches expected sequence
│   │                               │   ├── If matches, add to results and remove from pending collection
│   │                               │   └── Update current sequence and expected next sequence
│   │                               ├── Update cache with new current sequence
│   │                               └── Update or clean up cache and timer
│   │                                   ├── If pending messages remain and size changed ── Update cache and reset timer
│   │                                   └── If pending messages count is 0 ── Remove cache entry and dispose timer
│   └──
└── Return processed messages
```

### Timeout Handling Flow

```
OnReorderTimeout(object? state)
├── Validate and parse timer key to extract identifiers
├── Acquire lock for thread safety
│   ├── Remove and dispose timer to prevent duplicate callbacks
│   ├── Get all pending messages (with seq = -1 to indicate timeout mode)
│   │   ├── Build all required cache keys (seqKey, pendingKey, timerKey)
│   │   ├── Check if pending messages exist
│   │   ├── Process pending messages in sequential order (timeout mode still processes consecutive sequences)
│   │   │   ├── Loop until no more continuous messages found
│   │   │   ├── Process first message in pending collection (timeout mode bypasses sequence check)
│   │   │   ├── Add to results and remove from pending collection
│   │   │   └── Update current sequence and expected next sequence
│   │   ├── Update cache with new current sequence
│   │   └── Update or clean up cache
│   │       ├── If pending messages remain and size changed ── Update cache and reset timer
│   │       └── If pending messages count is 0 ── Remove cache entry (timer already disposed)
└── Exit lock
├── Call OnPendingMessages delegate outside lock if pending messages exist
└── Check if SendRebirthWhenTimeout is enabled
    └── YES ── Send rebirth request via OnRebirthRequested delegate
```

### Reorder Clearing Flow

```
ClearMessageOrder(groupId, edgeNodeId, deviceId)
├── Build all required cache keys (seqKey, pendingKey, timerKey)
├── Acquire fine-grained lock for thread safety on specific device/node combination
├── Remove sequence number cache entry
├── Remove pending messages cache entry
├── Remove entry from _cachedSeqKeys and _cachedPendingKeys collections
├── Remove and dispose timer if it exists
└── Exit lock
```

### Get All Messages and Clear Cache Flow

```
GetAllMessagesAndClearCache()
├── Obtain snapshot of timer keys to avoid concurrent modification exceptions
├── Dispose all reorder timers to prevent callbacks during cache clearing
│   ├── Try to remove each timer from _reorderTimers collection
│   ├── Call Dispose() on each removed timer
├── Initialize result list for all cached messages
├── Obtain snapshot of pending cache keys to avoid concurrent modification exceptions
├── Process each pending key
│   ├── Try to get pending messages from cache
│   ├── Add all pending messages to result list
│   ├── Remove pending messages from cache
├── Clear _cachedPendingKeys collection
├── Obtain snapshot of sequence cache keys to avoid concurrent modification exceptions
├── Remove each sequence key from cache
├── Clear _cachedSeqKeys collection
└── Return result list containing all previously cached messages
```
