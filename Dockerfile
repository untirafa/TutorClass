FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TutorClass.slnx ./
COPY TutorClass.Domain/TutorClass.Domain.csproj TutorClass.Domain/
COPY TutorClass.Application/TutorClass.Application.csproj TutorClass.Application/
COPY TutorClass.Infrastructure/TutorClass.Infrastructure.csproj TutorClass.Infrastructure/
COPY TutorClass.API/TutorClass.API.csproj TutorClass.API/
COPY TutorClass.Tests/TutorClass.Tests.csproj TutorClass.Tests/

RUN dotnet restore TutorClass.slnx

COPY . .
RUN dotnet publish TutorClass.API/TutorClass.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 10000

COPY --from=build /app/publish .

CMD ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000} dotnet TutorClass.API.dll
