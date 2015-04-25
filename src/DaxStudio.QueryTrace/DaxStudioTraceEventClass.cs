﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaxStudio.QueryTrace
{
    public enum DaxStudioTraceEventClass
    {
        NotAvailable = 0,
        AuditLogin = 1,
        AuditLogout = 2,
        AuditServerStartsAndStops = 4,
        ProgressReportBegin = 5,
        ProgressReportEnd = 6,
        ProgressReportCurrent = 7,
        ProgressReportError = 8,
        QueryBegin = 9,
        QueryEnd = 10,
        QuerySubcube = 11,
        QuerySubcubeVerbose = 12,
        CommandBegin = 15,
        CommandEnd = 16,
        Error = 17,
        AuditObjectPermission = 18,
        AuditAdminOperations = 19,
        ServerStateDiscoverBegin = 33,
        ServerStateDiscoverData = 34,
        ServerStateDiscoverEnd = 35,
        DiscoverBegin = 36,
        DiscoverEnd = 38,
        Notification = 39,
        UserDefined = 40,
        ExistingConnection = 41,
        ExistingSession = 42,
        SessionInitialize = 43,
        Deadlock = 50,
        Locktimeout = 51,
        LockAcquired = 52,
        LockReleased = 53,
        LockWaiting = 54,
        GetDataFromAggregation = 60,
        GetDataFromCache = 61,
        QueryCubeBegin = 70,
        QueryCubeEnd = 71,
        CalculateNonEmptyBegin = 72,
        CalculateNonEmptyCurrent = 73,
        CalculateNonEmptyEnd = 74,
        SerializeResultsBegin = 75,
        SerializeResultsCurrent = 76,
        SerializeResultsEnd = 77,
        ExecuteMdxScriptBegin = 78,
        ExecuteMdxScriptCurrent = 79,
        ExecuteMdxScriptEnd = 80,
        QueryDimension = 81,
        VertiPaqSEQueryBegin = 82,
        VertiPaqSEQueryEnd = 83,
        ResourceUsage = 84,
        VertiPaqSEQueryCacheMatch = 85,
        FileLoadBegin = 90,
        FileLoadEnd = 91,
        FileSaveBegin = 92,
        FileSaveEnd = 93,
        PageOutBegin = 94,
        PageOutEnd = 95,
        PageInBegin = 96,
        PageInEnd = 97,
        DirectQueryBegin = 98,
        DirectQueryEnd = 99,
        CalculationEvaluation = 110,
        CalculationEvaluationDetailedInformation = 111,
        DAXQueryPlan = 112,
    }
}
