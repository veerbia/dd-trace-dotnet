// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.

#pragma once
#include <list>
#include <memory>

// forward declarations
class SampleEnumerator;

class ISamplesProvider
{
public:
    virtual ~ISamplesProvider() = default;
    virtual std::unique_ptr<SampleEnumerator> GetSamples() = 0;
    virtual const char* GetName() = 0;
};