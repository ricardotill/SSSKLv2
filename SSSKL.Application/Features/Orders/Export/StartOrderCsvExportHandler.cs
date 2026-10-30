using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Orders.Export;

internal sealed class StartOrderCsvExportHandler(OrderCsvExportJob exportJob)
    : IRequestHandler<StartOrderCsvExportCommand, StartOrderCsvExportResult>
{
    public Task<StartOrderCsvExportResult> Handle(StartOrderCsvExportCommand request, CancellationToken cancellationToken)
    {
        var (job, started) = exportJob.StartExport(request.StartedByUserId);
        return Task.FromResult(new StartOrderCsvExportResult(job, started));
    }
}
