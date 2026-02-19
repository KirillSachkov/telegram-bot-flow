FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/TelegramBotFlow.Core/TelegramBotFlow.Core.csproj", "src/TelegramBotFlow.Core/"]
COPY ["src/TelegramBotFlow.Core.Data/TelegramBotFlow.Core.Data.csproj", "src/TelegramBotFlow.Core.Data/"]
COPY ["src/TelegramBotFlow.App/TelegramBotFlow.App.csproj", "src/TelegramBotFlow.App/"]
RUN dotnet restore "src/TelegramBotFlow.App/TelegramBotFlow.App.csproj"
COPY . .
WORKDIR "/src/src/TelegramBotFlow.App"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TelegramBotFlow.App.dll"]
