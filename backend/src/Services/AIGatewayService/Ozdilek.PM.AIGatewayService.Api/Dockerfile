FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Services/AIGatewayService/Ozdilek.PM.AIGatewayService.Api/Ozdilek.PM.AIGatewayService.Api.csproj"
RUN dotnet publish "src/Services/AIGatewayService/Ozdilek.PM.AIGatewayService.Api/Ozdilek.PM.AIGatewayService.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Ozdilek.PM.AIGatewayService.Api.dll"]
