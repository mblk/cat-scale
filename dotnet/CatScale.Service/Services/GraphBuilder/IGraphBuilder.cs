namespace CatScale.Service.Services.GraphBuilder;

public interface IGraphBuilder
{
    IGraphBuilder AddAxis(int axis, string label);

    IGraphBuilder AddDataset(GraphDataSet dataSet);

    IGraphBuilder AddBox(DateTimeOffset t1, DateTimeOffset t2, double v1, double v2, string label);

    Task<byte[]> Build();
}