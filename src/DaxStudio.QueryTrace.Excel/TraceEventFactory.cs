﻿extern alias ExcelAmo;

using xlAmo = ExcelAmo.Microsoft.AnalysisServices;
//using xlAmo = ExcelAmo.Microsoft.AnalysisServices;
namespace DaxStudio.QueryTrace
{
    public  static partial class TraceEventFactory
    {

        public static xlAmo.TraceEvent CreateExcelTrace(xlAmo.TraceEventClass eventClass)
        {
            xlAmo.TraceEvent trc = new xlAmo.TraceEvent(eventClass);
            trc.Columns.Add(xlAmo.TraceColumn.EventClass);
            trc.Columns.Add(xlAmo.TraceColumn.EventSubclass);
            trc.Columns.Add(xlAmo.TraceColumn.TextData);
            trc.Columns.Add(xlAmo.TraceColumn.CurrentTime);
            if (eventClass != xlAmo.TraceEventClass.VertiPaqSEQueryCacheMatch)
            {
                trc.Columns.Add(xlAmo.TraceColumn.StartTime);
            }
            trc.Columns.Add(xlAmo.TraceColumn.Spid);
            trc.Columns.Add(xlAmo.TraceColumn.SessionID);
            switch (eventClass)
            {
                case xlAmo.TraceEventClass.CommandEnd:
                case xlAmo.TraceEventClass.CalculateNonEmptyEnd:
                case xlAmo.TraceEventClass.DirectQueryEnd:
                case xlAmo.TraceEventClass.DiscoverEnd:
                case xlAmo.TraceEventClass.ExecuteMdxScriptEnd:
                case xlAmo.TraceEventClass.FileSaveEnd:
                case xlAmo.TraceEventClass.ProgressReportEnd:
                case xlAmo.TraceEventClass.QueryCubeEnd:
                case xlAmo.TraceEventClass.QueryEnd:
                case xlAmo.TraceEventClass.QuerySubcube:
                case xlAmo.TraceEventClass.QuerySubcubeVerbose:
                case xlAmo.TraceEventClass.VertiPaqSEQueryEnd:
                    trc.Columns.Add(xlAmo.TraceColumn.Duration);
                    trc.Columns.Add(xlAmo.TraceColumn.CpuTime);
                    break;
                case xlAmo.TraceEventClass.Error:
                    trc.Columns.Add(xlAmo.TraceColumn.Error);
                    break;

            }
            return trc;
        }
    }
}
