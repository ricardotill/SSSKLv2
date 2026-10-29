using SSSKL.Contracts.Dto.Api.v1;


namespace SSSKL.Contracts.Services;

public interface IOrderCsvExportJobService
{
    (CsvExportJobDto Job, bool Started) StartExport(string startedByUserId);
    CsvExportJobDto? GetStatus(Guid jobId);
    (byte[] Bytes, string FileName)? GetCsv(Guid jobId);
}
