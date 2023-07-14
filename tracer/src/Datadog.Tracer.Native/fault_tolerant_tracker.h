#ifndef DD_CLR_PROFILER_FAULT_TOLERANT_H_
#define DD_CLR_PROFILER_FAULT_TOLERANT_H_

#include "../../../shared/src/native-src/string.h"
#include "../../../shared/src/native-src/util.h"
#include "corhlpr.h"
#include "integration.h"
#include <corprof.h>
#include <mutex>
#include <unordered_map>

namespace fault_tolerant
{
class FaultTolerantTracker : public shared::Singleton<FaultTolerantTracker>
{
    friend class shared::Singleton<FaultTolerantTracker>;

private:
    std::recursive_mutex _faultTolerantMapMutex;
    
    std::unordered_map<trace::MethodIdentifier, std::tuple<trace::MethodIdentifier, trace::MethodIdentifier>>
        _faultTolerantMethods{};
    std::unordered_map < trace::MethodIdentifier, std::tuple<LPCBYTE, ULONG>> _methodBodies{};
    std::unordered_map<trace::MethodIdentifier, trace::MethodIdentifier> _originalMethods;
    std::unordered_map<trace::MethodIdentifier, trace::MethodIdentifier> _instrumentedMethods;

public:
    FaultTolerantTracker() = default;
    
    void AddFaultTolerant(ModuleID fromModuleId, mdMethodDef fromMethodId, mdMethodDef toOriginalMethodId,
                          mdMethodDef toInstrumentedMethodId);
    mdMethodDef GetOriginalMethod(ModuleID moduleId, mdMethodDef methodId);
    mdMethodDef GetInstrumentedMethod(ModuleID moduleId, mdMethodDef methodId);
    mdMethodDef GetKickoffMethodFromOriginalMethod(ModuleID moduleId, mdMethodDef methodId);
    mdMethodDef GetKickoffMethodFromInstrumentedMethod(ModuleID moduleId, mdMethodDef methodId);

    bool IsKickoffMethod(ModuleID moduleId, mdMethodDef methodId);
    bool IsOriginalMethod(ModuleID moduleId, mdMethodDef methodId);
    bool IsInstrumentedMethod(ModuleID moduleId, mdMethodDef methodId);
    void KeepILBodyAndSize(ModuleID moduleId, mdMethodDef methodId, LPCBYTE pMethodBytes, ULONG methodSize);
    std::tuple<LPCBYTE, ULONG> GetILBodyAndSize(ModuleID moduleId, mdMethodDef methodId);
};

} // namespace debugger

#endif