#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["sirus.kafka/sirus.kafka.consumer.csproj", "sirus.kafka/"]
RUN dotnet restore "sirus.kafka/sirus.kafka.consumer.csproj"
COPY . .
WORKDIR "/src/sirus.kafka"
RUN dotnet build "sirus.kafka.consumer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "sirus.kafka.consumer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "sirus.kafka.consumer.dll"]