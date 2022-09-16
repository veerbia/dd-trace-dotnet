// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.

#pragma once

//#include <cor.h> // for WCHAR
#include <cstdint>

enum GCReason
{
    AllocSmall,
    Induced,
    LowMemory,
    Empty,
    AllocLarge,
    OutOfSpaceSOH,
    OutOfSpaceLOH,
    InducedNotForced,
    Internal,
    InducedLowMemory,
    InducedCompacting,
    LowMemoryHost,
    PMFullGC,
    LowMemoryHostBlocking
};

enum GCType
{
    NonConcurrentGC,
    BackgroundGC,
    ForegroundGC
};

enum GCGlobalMechanisms
{
    None = 0x0,
    Concurrent = 0x1,
    Compaction = 0x2,
    Promotion = 0x4,
    Demotion = 0x8,
    CardBundles = 0x10
};

class IGarbageCollectionsListener
{
public:
    virtual void OnGarbageCollection(
        int32_t number,
        uint32_t generation,
        GCReason reason,
        GCType type,
        bool isCompacting,
        uint64_t pauseDuration,
        uint64_t timestamp) = 0;

    virtual ~IGarbageCollectionsListener() = default;
};
