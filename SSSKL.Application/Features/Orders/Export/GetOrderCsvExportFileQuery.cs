using MediatR;

namespace SSSKL.Application.Features.Orders.Export;

/// <summary>Returns the generated CSV, or null while the export is missing, running or failed.</summary>
public sealed record GetOrderCsvExportFileQuery(Guid JobId) : IRequest<CsvExportFile?>;

internal sealed class GetOrderCsvExportFileHandler(OrderCsvExportJob exportJob)
    : IRequestHandler<GetOrderCsvExportFileQuery, CsvExportFile?>
{
    public Task<CsvExportFile?> Handle(GetOrderCsvExportFileQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(exportJob.GetCsv(request.JobId));
}
