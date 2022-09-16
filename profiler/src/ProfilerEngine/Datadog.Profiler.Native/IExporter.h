// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.

#pragma once
#include <memory>

// forward declarations
class IProfile;
class Sample;

class IExporter
{
public:
    virtual ~IExporter() = default;
    virtual void Add(Sample const& sample) = 0;
    virtual void SetEndpoint(const std::string& runtimeId, uint64_t traceId, const std::string& endpoint) = 0;
    virtual bool Export(std::string filename, uint8_t* pBuffer, uint64_t bufferSize) = 0;
};