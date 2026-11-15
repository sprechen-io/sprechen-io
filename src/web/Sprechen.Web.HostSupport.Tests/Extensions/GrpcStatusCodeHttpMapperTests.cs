using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Sprechen.Web.HostSupport.Extensions;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Extensions;

public sealed class GrpcStatusCodeHttpMapperTests
{
    [Theory]
    [InlineData(StatusCode.InvalidArgument, StatusCodes.Status400BadRequest)]
    [InlineData(StatusCode.Unauthenticated, StatusCodes.Status401Unauthorized)]
    [InlineData(StatusCode.PermissionDenied, StatusCodes.Status403Forbidden)]
    [InlineData(StatusCode.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(StatusCode.AlreadyExists, StatusCodes.Status409Conflict)]
    [InlineData(StatusCode.FailedPrecondition, StatusCodes.Status412PreconditionFailed)]
    [InlineData(StatusCode.ResourceExhausted, StatusCodes.Status429TooManyRequests)]
    [InlineData(StatusCode.Cancelled, StatusCodes.Status502BadGateway)]
    [InlineData(StatusCode.Unavailable, StatusCodes.Status503ServiceUnavailable)]
    [InlineData(StatusCode.DeadlineExceeded, StatusCodes.Status504GatewayTimeout)]
    [InlineData(StatusCode.Internal, StatusCodes.Status500InternalServerError)]
    public void Map_MapsEachKnownStatusCode(StatusCode statusCode, int expectedHttpStatus)
    {
        Assert.Equal(expectedHttpStatus, new GrpcStatusCodeHttpMapper().Map(statusCode));
    }

    [Theory]
    [InlineData(StatusCode.Unimplemented)]
    [InlineData(StatusCode.Aborted)]
    [InlineData(StatusCode.OutOfRange)]
    [InlineData(StatusCode.DataLoss)]
    [InlineData(StatusCode.OK)]
    public void Map_UnmappedStatusCode_DefaultsTo502BadGateway(StatusCode statusCode)
    {
        Assert.Equal(StatusCodes.Status502BadGateway, new GrpcStatusCodeHttpMapper().Map(statusCode));
    }
}
