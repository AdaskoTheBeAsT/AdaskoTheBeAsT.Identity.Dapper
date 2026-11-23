using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Controllers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Mapping;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AwesomeAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Test;

public sealed class RolePaginationTest
{
    [Fact]
    public async Task HandlerUsesAsyncDatabasePagingAndForwardsCancellationAsync()
    {
        var roles = new Mock<IPagedRoleStore<ApplicationRole>>(MockBehavior.Strict);
        var mapper = new Mock<IWebApiMapper>(MockBehavior.Strict);
        IList<ApplicationRole> entities = new List<ApplicationRole> { new() { Id = Guid.NewGuid(), Name = "role" } };
        var models = new[] { new RoleModel { Name = "role" } };
        var cancellationToken = TestContext.Current.CancellationToken;
        roles.Setup(store => store.GetRolesPageAsync(12, 7, cancellationToken)).ReturnsAsync(entities);
        mapper.Setup(instance => instance.MapToRoleModels(entities)).Returns(models);
        var handler = new GetAllRolesRequestHandler(roles.Object, mapper.Object);

        (await handler.Handle(new GetAllRolesRequest { Offset = 12, PageSize = 7 }, cancellationToken)).Should().BeSameAs(models);
        roles.VerifyAll();
        mapper.VerifyAll();
    }

    [Fact]
    public async Task ControllerPassesBoundsToTheHandlerAsync()
    {
        var getAllHandler = new Mock<IRequestHandler<GetAllRolesRequest, IEnumerable<RoleModel>>>(MockBehavior.Strict);
        var models = Array.Empty<RoleModel>();
        var cancellationToken = TestContext.Current.CancellationToken;
        getAllHandler.Setup(instance => instance.Handle(
                It.Is<GetAllRolesRequest>(request => request.Offset == 12 && request.PageSize == 7),
                cancellationToken))
            .ReturnsAsync(models);
        var controller = new RoleController(
            Mock.Of<IWebApiMapper>(MockBehavior.Strict),
            Mock.Of<IRequestHandler<CreateRoleRequest, IdentityResult>>(MockBehavior.Strict),
            getAllHandler.Object);

        var response = (await controller.GetAllRolesAsync(12, 7, cancellationToken)).Should().BeOfType<OkObjectResult>().Which;
        response.Value.Should().BeSameAs(models);
        getAllHandler.VerifyAll();
    }

    [Fact]
    public Task ControllerDoesNotSwallowPagingCancellationAsync()
    {
        var getAllHandler = new Mock<IRequestHandler<GetAllRolesRequest, IEnumerable<RoleModel>>>(MockBehavior.Strict);
        getAllHandler.Setup(instance => instance.Handle(It.IsAny<GetAllRolesRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        var controller = new RoleController(
            Mock.Of<IWebApiMapper>(MockBehavior.Strict),
            Mock.Of<IRequestHandler<CreateRoleRequest, IdentityResult>>(MockBehavior.Strict),
            getAllHandler.Object);
        return FluentActions.Awaiting(() => controller.GetAllRolesAsync(cancellationToken: TestContext.Current.CancellationToken))
            .Should()
            .ThrowAsync<OperationCanceledException>();
    }
}
