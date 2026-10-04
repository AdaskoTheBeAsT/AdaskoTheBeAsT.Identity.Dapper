using AdaskoTheBeAsT.Identity.Dapper.Abstractions;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Controllers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Handlers;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Identity;
using AdaskoTheBeAsT.Identity.Dapper.WebApi.Models;
using AutoMapper;
using AwesomeAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AdaskoTheBeAsT.Identity.Dapper.WebApi.Test;

public sealed class RolePaginationTest
{
    [Fact]
    public async Task HandlerUsesAsyncDatabasePagingAndForwardsCancellation()
    {
        var roles = new Mock<IPagedRoleStore<ApplicationRole>>(MockBehavior.Strict);
        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        IList<ApplicationRole> entities = new List<ApplicationRole> { new() { Id = Guid.NewGuid(), Name = "role" } };
        var models = new[] { new RoleModel { Name = "role" } };
        var cancellationToken = TestContext.Current.CancellationToken;
        roles.Setup(store => store.GetRolesPageAsync(12, 7, cancellationToken)).ReturnsAsync(entities);
        mapper.Setup(instance => instance.Map<IEnumerable<RoleModel>>(entities)).Returns(models);
        var handler = new GetAllRolesRequestHandler(roles.Object, mapper.Object);

        (await handler.Handle(new GetAllRolesRequest { Offset = 12, PageSize = 7 }, cancellationToken)).Should().BeSameAs(models);
        roles.VerifyAll();
        mapper.VerifyAll();
    }

    [Fact]
    public async Task ControllerPassesBoundsToTheHandler()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var models = Array.Empty<RoleModel>();
        var cancellationToken = TestContext.Current.CancellationToken;
        mediator.Setup(instance => instance.Send(It.Is<GetAllRolesRequest>(request => request.Offset == 12 && request.PageSize == 7), cancellationToken))
            .ReturnsAsync(models);
        var controller = new RoleController(Mock.Of<IMapper>(MockBehavior.Strict), mediator.Object);

        var response = (await controller.GetAllRolesAsync(12, 7, cancellationToken)).Should().BeOfType<OkObjectResult>().Which;
        response.Value.Should().BeSameAs(models);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task ControllerDoesNotSwallowPagingCancellation()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator.Setup(instance => instance.Send(It.IsAny<GetAllRolesRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        var controller = new RoleController(Mock.Of<IMapper>(MockBehavior.Strict), mediator.Object);
        await FluentActions.Awaiting(() => controller.GetAllRolesAsync(cancellationToken: TestContext.Current.CancellationToken)).Should().ThrowAsync<OperationCanceledException>();
    }
}
