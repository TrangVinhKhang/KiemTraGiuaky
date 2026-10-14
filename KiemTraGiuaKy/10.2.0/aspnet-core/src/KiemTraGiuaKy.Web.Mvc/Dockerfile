FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
RUN ln -s ~/.dotnet/tools/libman /usr/local/bin/libman

WORKDIR /src
COPY ["src/KiemTraGiuaKy.Web.Mvc/KiemTraGiuaKy.Web.Mvc.csproj", "src/KiemTraGiuaKy.Web.Mvc/"]
COPY ["src/KiemTraGiuaKy.Web.Core/KiemTraGiuaKy.Web.Core.csproj", "src/KiemTraGiuaKy.Web.Core/"]
COPY ["src/KiemTraGiuaKy.Application/KiemTraGiuaKy.Application.csproj", "src/KiemTraGiuaKy.Application/"]
COPY ["src/KiemTraGiuaKy.Core/KiemTraGiuaKy.Core.csproj", "src/KiemTraGiuaKy.Core/"]
COPY ["src/KiemTraGiuaKy.EntityFrameworkCore/KiemTraGiuaKy.EntityFrameworkCore.csproj", "src/KiemTraGiuaKy.EntityFrameworkCore/"]
WORKDIR "/src/src/KiemTraGiuaKy.Web.Mvc"
RUN dotnet restore 

WORKDIR /src
COPY ["src/KiemTraGiuaKy.Web.Mvc", "src/KiemTraGiuaKy.Web.Mvc"]
COPY ["src/KiemTraGiuaKy.Web.Core", "src/KiemTraGiuaKy.Web.Core"]
COPY ["src/KiemTraGiuaKy.Application", "src/KiemTraGiuaKy.Application"]
COPY ["src/KiemTraGiuaKy.Core", "src/KiemTraGiuaKy.Core"]
COPY ["src/KiemTraGiuaKy.EntityFrameworkCore", "src/KiemTraGiuaKy.EntityFrameworkCore"]
WORKDIR "/src/src/KiemTraGiuaKy.Web.Mvc"
RUN libman restore
RUN dotnet publish -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 80
WORKDIR /app
COPY --from=build /publish .
ENTRYPOINT ["dotnet", "KiemTraGiuaKy.Web.Mvc.dll"]
