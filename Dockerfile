# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/EnterpriseRag/EnterpriseRag.csproj src/EnterpriseRag/
RUN dotnet restore src/EnterpriseRag/EnterpriseRag.csproj
COPY src/EnterpriseRag/ src/EnterpriseRag/
RUN dotnet publish src/EnterpriseRag/EnterpriseRag.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
RUN groupadd --system rag && useradd --system --gid rag rag
COPY --from=build /app/publish/ ./
COPY samples/ ./samples/
COPY evals/ ./evals/
USER rag
ENTRYPOINT ["dotnet", "EnterpriseRag.dll"]
CMD ["ask", "How should latency incidents be handled?"]
