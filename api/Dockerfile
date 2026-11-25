FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RagApi/RagApi.csproj RagApi/
RUN dotnet restore RagApi/RagApi.csproj
COPY RagApi/ RagApi/
RUN dotnet publish RagApi/RagApi.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .
RUN mkdir /data && chown app /data
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "RagApi.dll"]