FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app


COPY ["CommandService.csproj", "./"]
RUN dotnet restore "CommandService.csproj"
COPY . .

RUN dotnet build "CommandService.csproj" -c Release -o out


FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "CommandService.dll"]
