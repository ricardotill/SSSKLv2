using MediatR;

namespace SSSKL.Application.Features.Orders.Export;

internal sealed class GetOrderCsvExportFileHandler(OrderCsvExportJob exportJob)
    : IRequestHandler<GetOrderCsvExportFileQuery, CsvExportFile?>
{
    public Task<CsvExportFile?> Handle(GetOrderCsvExportFileQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(exportJob.GetCsv(request.JobId));
}
