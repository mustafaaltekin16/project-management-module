FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Services/FeasibilityService/Ozdilek.PM.FeasibilityService.Api/Ozdilek.PM.FeasibilityService.Api.csproj"
RUN dotnet publish "src/Services/FeasibilityService/Ozdilek.PM.FeasibilityService.Api/Ozdilek.PM.FeasibilityService.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Ozdilek.PM.FeasibilityService.Api.dll"]
