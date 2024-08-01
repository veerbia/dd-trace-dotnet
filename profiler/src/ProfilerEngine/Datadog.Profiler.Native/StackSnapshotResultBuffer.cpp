// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.

#include "StackSnapshotResultBuffer.h"

StackSnapshotResultBuffer::StackSnapshotResultBuffer() :
    _unixTimeUtc{0},
    _representedDurationNanoseconds{0},
    _localRootSpanId{0},
    _spanId{0},
    _callstack{},
    _cacheCallstacks{false}
{
}

StackSnapshotResultBuffer::~StackSnapshotResultBuffer()
{
    _unixTimeUtc = 0;
    _representedDurationNanoseconds = 0;
    _localRootSpanId = 0;
    _spanId = 0;
    _cacheCallstacks = false;
}

void StackSnapshotResultBuffer::Reset()
{
    _localRootSpanId = 0;
    _spanId = 0;
    _representedDurationNanoseconds = 0;
    _unixTimeUtc = 0;
    _callstack = {};
    _cacheCallstacks = false;
}
