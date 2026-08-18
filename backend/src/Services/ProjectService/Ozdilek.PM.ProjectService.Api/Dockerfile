FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Services/ProjectService/Ozdilek.PM.ProjectService.Api/Ozdilek.PM.ProjectService.Api.csproj"
RUN dotnet publish "src/Services/ProjectService/Ozdilek.PM.ProjectService.Api/Ozdilek.PM.ProjectService.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Ozdilek.PM.ProjectService.Api.dll"]
