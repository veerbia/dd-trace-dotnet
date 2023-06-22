﻿// <copyright file="PublicApiUsage.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Datadog.Trace.SourceGenerators;

namespace Datadog.Trace.Telemetry.Metrics;

[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1134:Attributes should not share line", Justification = "It's easier to read")]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "It's easier to read")]
[EnumExtensions]
internal enum PublicApiUsage
{
    [Description("name:eventtrackingsdk_trackcustomevent")]EventTrackingSdk_TrackCustomEvent,
    [Description("name:eventtrackingsdk_trackcustomevent_metadata")]EventTrackingSdk_TrackCustomEvent_Metadata,
    [Description("name:eventtrackingsdk_trackuserloginfailureevent")]EventTrackingSdk_TrackUserLoginFailureEvent,
    [Description("name:eventtrackingsdk_trackuserloginfailureevent_metadata")]EventTrackingSdk_TrackUserLoginFailureEvent_Metadata,
    [Description("name:eventtrackingsdk_trackuserloginsuccessevent")]EventTrackingSdk_TrackUserLoginSuccessEvent,
    [Description("name:eventtrackingsdk_trackuserloginsuccessevent_metadata")]EventTrackingSdk_TrackUserLoginSuccessEvent_Metadata,

    [Description("name:spancontextextractor_extract")] SpanContextExtractor_Extract,

    [Description("name:spanextensions_setuser")] SpanExtensions_SetUser,
    [Description("name:spanextensions_settracesamplingpriority")] SpanExtensions_SetTraceSamplingPriority,

    [Description("name:tracer_ctor")] Tracer_Ctor,
    [Description("name:tracer_ctor_settings")] Tracer_Ctor_Settings,
    [Description("name:tracer_instance_set")] Tracer_Instance_Set,
    [Description("name:tracer_configure")] Tracer_Configure,
    [Description("name:tracer_forceflushasync")] Tracer_ForceFlushAsync,
    [Description("name:tracer_startactive")] Tracer_StartActive,
    [Description("name:tracer_startactive_settings")] Tracer_StartActive_Settings,
    [Description("name:itracer_startactive")] ITracer_StartActive,
    [Description("name:itracer_startactive_settings")] ITracer_StartActive_Settings,

    // These are problematic, as we use them _everywhere_ so means a lot of code changes
    // [Description("name:tracer_instance_get")] Tracer_Instance_Get,
    // [Description("name:tracer_activescope_get")] Tracer_ActiveScope_Get,
    // [Description("name:tracer_defaultservicename_get")] Tracer_DefaultServiceName_Get,
    // [Description("name:tracer_settings_get")] Tracer_Settings_Get,

    // These are problematic as we need to use them internally in some cases (version conflict)
    // [Description("name:iscope_span")] IScope_Span,
    // [Description("name:iscope_close")] IScope_Close,
    // [Description("name:ispan_operationname_get")] ISpan_OperationName_Get,
    // [Description("name:ispan_operationname_set")] ISpan_OperationName_Set,
    // [Description("name:ispan_resourcename_get")] ISpan_ResourceName_Get,
    // [Description("name:ispan_resourcename_set")] ISpan_ResourceName_Set,
    // [Description("name:ispan_type_get")] ISpan_Type_Get,
    // [Description("name:ispan_type_set")] ISpan_Type_Set,
    // [Description("name:ispan_error_get")] ISpan_Error_Get,
    // [Description("name:ispan_error_set")] ISpan_Error_Set,
    // [Description("name:ispan_servicename_get")] ISpan_ServiceName_Get,
    // [Description("name:ispan_servicename_set")] ISpan_ServiceName_Set,
    // [Description("name:ispan_traceid_get")] ISpan_TraceId_Get,
    // [Description("name:ispan_spanid_get")] ISpan_SpanId_Get,
    // [Description("name:ispan_context_get")] ISpan_Context_Get,
    // [Description("name:ispan_settag")] ISpan_SetTag,
    // [Description("name:ispan_finish")] ISpan_Finish,
    // [Description("name:ispan_finish_datetimeoffset")] ISpan_Finish_DateTimeOffset,
    // [Description("name:ispan_setexception")] ISpan_SetException,
    // [Description("name:ispan_gettag")] ISpan_GetTag,

    [Description("name:correlationidentifier_env_get")]Correlation_Identifier_Env_Get,
    [Description("name:correlationidentifier_service_get")]Correlation_Identifier_Service_Get,
    [Description("name:correlationidentifier_spanid_get")]Correlation_Identifier_SpanId_Get,
    [Description("name:correlationidentifier_traceid_get")]Correlation_Identifier_TraceId_Get,
    [Description("name:correlationidentifier_version_get")]Correlation_Identifier_Version_Get,

    [Description("name:spancontext_ctor")] SpanContext_Ctor,
    [Description("name:spancontext_parent_get")] SpanContext_Parent_Get,
    [Description("name:spancontext_parentid_get")] SpanContext_ParentId_Get,
    [Description("name:spancontext_servicename_get")] SpanContext_ServiceName_Get,
    [Description("name:spancontext_servicename_set")] SpanContext_ServiceName_Set,
    // These are problematic as they're used in a _lot_ of places
    // [Description("name:spancontext_spanid_get")] SpanContext_SpanId_Get,
    // [Description("name:spancontext_traceid_get")] SpanContext_TraceId_Get,

    [Description("name:exportersettings_ctor")] ExporterSettings_Ctor,
    [Description("name:exportersettings_ctor_source")] ExporterSettings_Ctor_Source,
    [Description("name:exportersettings_agenturi_get")] ExporterSettings_AgentUri_Get,
    [Description("name:exportersettings_agenturi_set")] ExporterSettings_AgentUri_Set,
    [Description("name:exportersettings_dogstatsdport_get")] ExporterSettings_DogStatsdPort_Get,
    [Description("name:exportersettings_dogstatsdport_set")] ExporterSettings_DogStatsdPort_Set,
    [Description("name:exportersettings_metricspipename_get")] ExporterSettings_MetricsPipeName_Get,
    [Description("name:exportersettings_metricspipename_set")] ExporterSettings_MetricsPipeName_Set,
    [Description("name:exportersettings_metricsunixdomainsocketpath_get")] ExporterSettings_MetricsUnixDomainSocketPath_Get,
    [Description("name:exportersettings_metricsunixdomainsocketpath_set")] ExporterSettings_MetricsUnixDomainSocketPath_Set,
    [Description("name:exportersettings_partialflushenabled_get")] ExporterSettings_PartialFlushEnabled_Get,
    [Description("name:exportersettings_partialflushenabled_set")] ExporterSettings_PartialFlushEnabled_Set,
    [Description("name:exportersettings_partialflushminspans_get")] ExporterSettings_PartialFlushMinSpans_Get,
    [Description("name:exportersettings_partialflushminspans_set")] ExporterSettings_PartialFlushMinSpans_Set,
    [Description("name:exportersettings_tracespipename_get")] ExporterSettings_TracesPipeName_Get,
    [Description("name:exportersettings_tracespipename_set")] ExporterSettings_TracesPipeName_Set,
    [Description("name:exportersettings_tracespipetimeoutms_get")] ExporterSettings_TracesPipeTimeoutMs_Get,
    [Description("name:exportersettings_tracespipetimeoutms_set")] ExporterSettings_TracesPipeTimeoutMs_Set,
    [Description("name:exportersettings_tracesunixdomainsocketpath_get")] ExporterSettings_TracesUnixDomainSocketPath_Get,
    [Description("name:exportersettings_tracesunixdomainsocketpath_set")] ExporterSettings_TracesUnixDomainSocketPath_Set,

    [Description("name:globalsettings_debugenabled_get")] GlobalSettings_DebugEnabled_Get,
    [Description("name:globalsettings_fromdefaultsources")] GlobalSettings_FromDefaultSources,
    [Description("name:globalsettings_reload")] GlobalSettings_Reload,
    [Description("name:globalsettings_setdebugenabled")] GlobalSettings_SetDebugEnabled,

    [Description("name:immutableexportersettings_ctor_settings")] ImmutableExporterSettings_Ctor_Settings,
    [Description("name:immutableexportersettings_ctor_source")] ImmutableExporterSettings_Ctor_Source,
    [Description("name:immutableexportersettings_agenturi_get")] ImmutableExporterSettings_AgentUri_Get,
    [Description("name:immutableexportersettings_dogstatsdport_get")] ImmutableExporterSettings_DogStatsdPort_Get,
    [Description("name:immutableexportersettings_metricspipename_get")] ImmutableExporterSettings_MetricsPipeName_Get,
    [Description("name:immutableexportersettings_metricsunixdomainsocketpath_get")] ImmutableExporterSettings_MetricsUnixDomainSocketPath_Get,
    [Description("name:immutableexportersettings_partialflushenabled_get")] ImmutableExporterSettings_PartialFlushEnabled_Get,
    [Description("name:immutableexportersettings_partialflushminspans_get")] ImmutableExporterSettings_PartialFlushMinSpans_Get,
    [Description("name:immutableexportersettings_tracespipename_get")] ImmutableExporterSettings_TracesPipeName_Get,
    [Description("name:immutableexportersettings_tracespipetimeoutms_get")] ImmutableExporterSettings_TracesPipeTimeoutMs_Get,
    [Description("name:immutableexportersettings_tracesunixdomainsocketpath_get")] ImmutableExporterSettings_TracesUnixDomainSocketPath_Get,

    [Description("name:integrationsettings_ctor")] IntegrationSettings_Ctor,
    [Description("name:integrationsettings_analyticsenabled_get")] IntegrationSettings_AnalyticsEnabled_Get,
    [Description("name:integrationsettings_analyticsenabled_set")] IntegrationSettings_AnalyticsEnabled_Set,
    [Description("name:integrationsettings_analyticssamplerate_get")] IntegrationSettings_AnalyticsSampleRate_Get,
    [Description("name:integrationsettings_analyticssamplerate_set")] IntegrationSettings_AnalyticsSampleRate_Set,
    [Description("name:integrationsettings_enabled_get")] IntegrationSettings_Enabled_Get,
    [Description("name:integrationsettings_enabled_set")] IntegrationSettings_Enabled_Set,
    [Description("name:integrationsettings_integrationname_get")] IntegrationSettings_IntegrationName_Get,

    [Description("name:integrationsettingscollection_ctor_source")] IntegrationSettingsCollection_Ctor_Source,
    [Description("name:integrationsettingscollection_indexer_name")] IntegrationSettingsCollection_Indexer_Name,

    [Description("name:immutableintegrationsettings_analyticsenabled_get")] ImmutableIntegrationSettings_AnalyticsEnabled_Get,
    [Description("name:immutableintegrationsettings_analyticssamplerate_get")] ImmutableIntegrationSettings_AnalyticsSampleRate_Get,
    [Description("name:immutableintegrationsettings_enabled_get")] ImmutableIntegrationSettings_Enabled_Get,
    [Description("name:immutableintegrationsettings_integrationname_get")] ImmutableIntegrationSettings_IntegrationName_Get,
    [Description("name:immutableintegrationsettingscollection_indexer_name")] ImmutableIntegrationSettingsCollection_Indexer_Name,

    [Description("name:compositeconfigurationsource_ctor")] CompositeConfigurationSource_Ctor,
    [Description("name:compositeconfigurationsource_add")] CompositeConfigurationSource_Add,
    [Description("name:compositeconfigurationsource_insert")] CompositeConfigurationSource_Insert,
    [Description("name:jsonconfigurationsource_ctor_json")] JsonConfigurationSource_Ctor_Json,
    [Description("name:jsonconfigurationsource_fromfile")] JsonConfigurationSource_FromFile,
    [Description("name:stringconfigurationsource_parsecustomkeyvalues_data")] StringConfigurationSource_ParseCustomKeyValues,
    [Description("name:stringconfigurationsource_parsecustomkeyvalues_allowoptionalmappings")] StringConfigurationSource_ParseCustomKeyValues_AllowOptionalMappings,
    [Description("name:customtelemeteredconfigurationsource_ctor")] CustomTelemeteredConfigurationSource_Ctor,
    [Description("name:environmentconfigurationsource_ctor")] EnvironmentConfigurationSource_Ctor,
    [Description("name:namevalueconfigurationsource_ctor")] NameValueConfigurationSource_Ctor,

    [Description("name:tracersettings_ctor")] TracerSettings_Ctor,
    [Description("name:tracersettings_ctor_source")] TracerSettings_Ctor_Source,
    [Description("name:tracersettings_ctor_usedefaultsources")] TracerSettings_Ctor_UseDefaultSources,
    [Description("name:tracersettings_analyticsenabled_get")] TracerSettings_AnalyticsEnabled_Get,
    [Description("name:tracersettings_analyticsenabled_set")] TracerSettings_AnalyticsEnabled_Set,
    [Description("name:tracersettings_customsamplingrules_get")] TracerSettings_CustomSamplingRules_Get,
    [Description("name:tracersettings_customsamplingrules_set")] TracerSettings_CustomSamplingRules_Set,
    [Description("name:tracersettings_diagnosticsourceenabled_get")] TracerSettings_DiagnosticSourceEnabled_Get,
    [Description("name:tracersettings_diagnosticsourceenabled_set")] TracerSettings_DiagnosticSourceEnabled_Set,
    [Description("name:tracersettings_disabledintegrationnames_get")] TracerSettings_DisabledIntegrationNames_Get,
    [Description("name:tracersettings_disabledintegrationnames_set")] TracerSettings_DisabledIntegrationNames_Set,
    [Description("name:tracersettings_environment_get")] TracerSettings_Environment_Get,
    [Description("name:tracersettings_environment_set")] TracerSettings_Environment_Set,
    [Description("name:tracersettings_exporter_get")] TracerSettings_Exporter_Get,
    [Description("name:tracersettings_exporter_set")] TracerSettings_Exporter_Set,
    [Description("name:tracersettings_globalsamplingrate_get")] TracerSettings_GlobalSamplingRate_Get,
    [Description("name:tracersettings_globalsamplingrate_set")] TracerSettings_GlobalSamplingRate_Set,
    [Description("name:tracersettings_globaltags_get")] TracerSettings_GlobalTags_Get,
    [Description("name:tracersettings_globaltags_set")] TracerSettings_GlobalTags_Set,
    [Description("name:tracersettings_grpctags_get")] TracerSettings_GrpcTags_Get,
    [Description("name:tracersettings_grpctags_set")] TracerSettings_GrpcTags_Set,
    [Description("name:tracersettings_headertags_get")] TracerSettings_HeaderTags_Get,
    [Description("name:tracersettings_headertags_set")] TracerSettings_HeaderTags_Set,
    [Description("name:tracersettings_integrations_get")] TracerSettings_Integrations_Get,
    [Description("name:tracersettings_kafkacreateconsumerscopeenabled_get")] TracerSettings_KafkaCreateConsumerScopeEnabled_Get,
    [Description("name:tracersettings_kafkacreateconsumerscopeenabled_set")] TracerSettings_KafkaCreateConsumerScopeEnabled_Set,
    [Description("name:tracersettings_logsinjectionenabled_get")] TracerSettings_LogsInjectionEnabled_Get,
    [Description("name:tracersettings_logsinjectionenabled_set")] TracerSettings_LogsInjectionEnabled_Set,
    [Description("name:tracersettings_maxtracessubmittedpersecond_get")] TracerSettings_MaxTracesSubmittedPerSecond_Get,
    [Description("name:tracersettings_maxtracessubmittedpersecond_set")] TracerSettings_MaxTracesSubmittedPerSecond_Set,
    [Description("name:tracersettings_servicename_get")] TracerSettings_ServiceName_Get,
    [Description("name:tracersettings_servicename_set")] TracerSettings_ServiceName_Set,
    [Description("name:tracersettings_serviceversion_get")] TracerSettings_ServiceVersion_Get,
    [Description("name:tracersettings_serviceversion_set")] TracerSettings_ServiceVersion_Set,
    [Description("name:tracersettings_startupdiagnosticlogenabled_get")] TracerSettings_StartupDiagnosticLogEnabled_Get,
    [Description("name:tracersettings_startupdiagnosticlogenabled_set")] TracerSettings_StartupDiagnosticLogEnabled_Set,
    [Description("name:tracersettings_statscomputationenabled_get")] TracerSettings_StatsComputationEnabled_Get,
    [Description("name:tracersettings_statscomputationenabled_set")] TracerSettings_StatsComputationEnabled_Set,
    [Description("name:tracersettings_traceenabled_get")] TracerSettings_TraceEnabled_Get,
    [Description("name:tracersettings_traceenabled_set")] TracerSettings_TraceEnabled_Set,
    [Description("name:tracersettings_tracermetricsenabled_get")] TracerSettings_TracerMetricsEnabled_Get,
    [Description("name:tracersettings_tracermetricsenabled_set")] TracerSettings_TracerMetricsEnabled_Set,
    [Description("name:tracersettings_build")] TracerSettings_Build,
    [Description("name:tracersettings_sethttpclienterrorstatuscodes")] TracerSettings_SetHttpClientErrorStatusCodes,
    [Description("name:tracersettings_sethttpservererrorstatuscodes")] TracerSettings_SetHttpServerErrorStatusCodes,
    [Description("name:tracersettings_setservicenamemappings")] TracerSettings_SetServiceNameMappings,
    [Description("name:tracersettings_createdefaultconfigurationsource")] TracerSettings_CreateDefaultConfigurationSource,
    [Description("name:tracersettings_fromdefaultsources")] TracerSettings_FromDefaultSources,

    [Description("name:immutabletracersettings_ctor_source")] ImmutableTracerSettings_Ctor_Source,
    [Description("name:immutabletracersettings_ctor_settings")] ImmutableTracerSettings_Ctor_Settings,
    [Description("name:immutabletracersettings_analyticsenabled_get")] ImmutableTracerSettings_AnalyticsEnabled_Get,
    [Description("name:immutabletracersettings_customsamplingrules_get")] ImmutableTracerSettings_CustomSamplingRules_Get,
    [Description("name:immutabletracersettings_environment_get")] ImmutableTracerSettings_Environment_Get,
    [Description("name:immutabletracersettings_exporter_get")] ImmutableTracerSettings_Exporter_Get,
    [Description("name:immutabletracersettings_globalsamplingrate_get")] ImmutableTracerSettings_GlobalSamplingRate_Get,
    [Description("name:immutabletracersettings_globaltags_get")] ImmutableTracerSettings_GlobalTags_Get,
    [Description("name:immutabletracersettings_grpctags_get")] ImmutableTracerSettings_GrpcTags_Get,
    [Description("name:immutabletracersettings_headertags_get")] ImmutableTracerSettings_HeaderTags_Get,
    [Description("name:immutabletracersettings_integrations_get")] ImmutableTracerSettings_Integrations_Get,
    [Description("name:immutabletracersettings_kafkacreateconsumerscopeenabled_get")] ImmutableTracerSettings_KafkaCreateConsumerScopeEnabled_Get,
    [Description("name:immutabletracersettings_logsinjectionenabled_get")] ImmutableTracerSettings_LogsInjectionEnabled_Get,
    [Description("name:immutabletracersettings_maxtracessubmittedpersecond_get")] ImmutableTracerSettings_MaxTracesSubmittedPerSecond_Get,
    [Description("name:immutabletracersettings_servicename_get")] ImmutableTracerSettings_ServiceName_Get,
    [Description("name:immutabletracersettings_serviceversion_get")] ImmutableTracerSettings_ServiceVersion_Get,
    [Description("name:immutabletracersettings_startupdiagnosticlogenabled_get")] ImmutableTracerSettings_StartupDiagnosticLogEnabled_Get,
    [Description("name:immutabletracersettings_statscomputationenabled_get")] ImmutableTracerSettings_StatsComputationEnabled_Get,
    [Description("name:immutabletracersettings_traceenabled_get")] ImmutableTracerSettings_TraceEnabled_Get,
    [Description("name:immutabletracersettings_tracermetricsenabled_get")] ImmutableTracerSettings_TracerMetricsEnabled_Get,
    [Description("name:immutabletracersettings_fromdefaultsources")] ImmutableTracerSettings_FromDefaultSources,

    [Description("name:opentracingtracerfactory_createtracer")] OpenTracingTracerFactory_CreateTracer,
    [Description("name:opentracingtracerfactory_wraptracer")] OpenTracingTracerFactory_WrapTracer,
    [Description("name:opentracingtracer_ctor_datadogtracer")] OpenTracingTracer_Ctor_DatadogTracer,
    [Description("name:opentracingtracer_ctor_datadogtracer_scopemanager")] OpenTracingTracer_Ctor_DatadogTracer_ScopeManager,
}