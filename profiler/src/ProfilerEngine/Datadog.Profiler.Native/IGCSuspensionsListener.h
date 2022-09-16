// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.

#pragma once

#include <cstdint>

class IGCSuspensionsListener
{
public:
    virtual void OnSuspension(
        int32_t Number,
        uint32_t Generation,
        uint64_t PauseDuration,
        uint64_t Timestamp) = 0;

    virtual ~IGCSuspensionsListener() = default;
};
