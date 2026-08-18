using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ozdilek.PM.AIGatewayService.Application.Interfaces;
using Ozdilek.PM.AIGatewayService.Application.Services;
using Ozdilek.PM.AIGatewayService.Infrastructure.Clients;
using Ozdilek.PM.AIGatewayService.Infrastructure.Persistence;
using Ozdilek.PM.AIGatewayService.Infrastructure.Providers;
using Ozdilek.PM.AIGatewayService.Infrastructure.Security;
using Ozdilek.PM.BuildingBlocks.Messaging;
using Ozdilek.PM.BuildingBlocks.Web;
using Ozdilek.PM.SharedKernel.Persistence;

namespace Ozdilek.PM.AIGatewayService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAIGatewayServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AIGatewayDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("AiGatewayDatabase")));

        services.AddScoped<IAiSuggestionRequestRepository, AiSuggestionRequestRepository>();
        services.AddScoped<IPromptTemplateRepository, PromptTemplateRepository>();
        services.AddScoped<IUnitOfWork, AIGatewayUnitOfWork>();
        services.AddScoped<IPromptAuditLogger, PromptAuditLogger>();
        services.AddScoped<AiSuggestionAppService>();
        services.AddSingleton<WorkPackageGenerationLockRegistry>();
        services.AddSingleton<ProjectSyncLockRegistry>();
        services.AddScoped<IRagDocumentSyncService, RagDocumentSyncService>();
        services.AddScoped<IWorkPackageContextRetrievalService, WorkPackageContextRetrievalService>();
        services.AddScoped<AiChatAppService>();

        var excerptOptions = configuration.GetSection(DocumentExcerptOptions.SectionName).Get<DocumentExcerptOptions>() ?? new DocumentExcerptOptions();
        services.AddSingleton(excerptOptions);

        var ragOptions = configuration.GetSection(RagOptions.SectionName).Get<RagOptions>() ?? new RagOptions();
        services.AddSingleton(ragOptions);

        var workPackageContextRetrievalOptions = configuration.GetSection(WorkPackageContextRetrievalOptions.SectionName)
            .Get<WorkPackageContextRetrievalOptions>() ?? new WorkPackageContextRetrievalOptions();
        services.AddSingleton(workPackageContextRetrievalOptions);

        services.AddCwaMessaging(configuration);

        var aiOptions = configuration.GetSection(AiProviderOptions.SectionName).Get<AiProviderOptions>() ?? new AiProviderOptions();
        services.AddSingleton(aiOptions);

        switch (aiOptions.Provider)
        {
            case "Mock":
                services.AddSingleton<ILlmProvider, MockLlmProvider>();
                break;
            default:
                // RagLlmProvider depends on IRagClient (registered below) rather than its own HttpClient,
                // so it's a plain service registration, not AddHttpClient<>.
                services.AddScoped<ILlmProvider, RagLlmProvider>();
                break;
        }

        services.AddHttpContextAccessor();
        services.AddTransient<BearerTokenForwardingHandler>();
        services.AddHttpClient<IProjectInfoClient, ProjectServiceClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:ProjectService"] ?? "http://projectservice-api:8080");
        }).AddHttpMessageHandler<BearerTokenForwardingHandler>();

        services.AddHttpClient<ITaskDocumentClient, TaskDocumentClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:TaskService"] ?? "http://taskservice-api:8080");
        }).AddHttpMessageHandler<BearerTokenForwardingHandler>();

        services.AddHttpClient<ITaskInfoClient, TaskInfoClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Services:TaskService"] ?? "http://taskservice-api:8080");
        }).AddHttpMessageHandler<BearerTokenForwardingHandler>();

        // Deliberately NOT .AddHttpMessageHandler<BearerTokenForwardingHandler>() — RAG is a separately
        // deployed service (RunPod/vLLM) with its own, unrelated optional X-API-Key auth, not this app's JWT.
        services.AddHttpClient<IRagClient, RagClient>(client =>
        {
            client.BaseAddress = new Uri(ragOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(100); // vLLM generation + multipart upload can be slow
            if (!string.IsNullOrWhiteSpace(ragOptions.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", ragOptions.ApiKey);
            }
        });

        return services;
    }
}
