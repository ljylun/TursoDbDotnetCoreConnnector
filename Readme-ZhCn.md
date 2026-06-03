# ToursDb

[English Version ReadMe](Readme.md)

一个演示项目，展示如何通过 HTTP API 将 **Entity Framework Core** 与 **Turso**（分布式 SQLite 数据库）集成。该项目实现了一个自定义 ADO.NET 数据提供程序，将所有 SQL 执行路由到 Turso 的 Hrana 协议，从而实现对无服务器 SQLite 数据库的无缝 EF Core 操作。

## 目录

- [项目概述](#项目概述)
- [技术栈](#技术栈)
- [环境要求](#环境要求)
- [项目结构](#项目结构)
- [快速开始](#快速开始)
- [配置说明](#配置说明)
- [开发规范](#开发规范)
- [常见问题排查](#常见问题排查)
- [许可证](#许可证)

## 项目概述

ToursDb 是一个 .NET 8 控制台应用程序，演示如何使用 Entity Framework Core 与 Turso 分布式 SQLite 数据库服务。该项目实现了一个自定义数据提供程序，将 EF Core 操作转换为对 Turso Hrana 协议的 HTTP 请求。

### 核心特性

- **EF Core 集成**：完整的 Entity Framework Core 支持，后端使用 Turso
- **自定义 ADO.NET 提供程序**：为 Turso HTTP 实现 `DbConnection`、`DbCommand`、`DbDataReader` 和 `DbTransaction`
- **Hrana 协议**：使用 Turso 基于 HTTP 的 Hrana 协议（v2 pipeline）进行数据库操作
- **CRUD 操作**：完整的增删改查演示
- **跨平台**：支持 Windows、macOS 和 Linux

## 技术栈

### 后端

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 8.0 | 运行时和框架 |
| C# | 12.0 | 主要编程语言 |
| Entity Framework Core | 8.0.11 | 数据库 ORM |
| Microsoft.Data.Sqlite | 8.0.11 | SQLite ADO.NET 提供程序（自定义实现的基础） |

### 基础设施

| 技术 | 版本 | 用途 |
|------|------|------|
| Turso | N/A | 分布式 SQLite 数据库服务 |
| Hrana 协议 | v2 | 基于 HTTP 的数据库协议 |
| libSQL | N/A | Turso 使用的 SQLite 分支 |

### 工具链

| 技术 | 版本 | 用途 |
|------|------|------|
| MSBuild | 17.x | 构建系统 |
| NuGet | 6.x | 包管理 |
| Visual Studio | 2022+ | IDE（可选） |
| .NET CLI | 8.0 | 命令行工具 |

### NuGet 包

| 包 | 版本 | 用途 |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore | 8.0.11 | EF Core 核心功能 |
| Microsoft.EntityFrameworkCore.Relational | 8.0.11 | 关系数据库支持 |
| Microsoft.EntityFrameworkCore.Sqlite | 8.0.11 | EF Core SQLite 提供程序 |
| Microsoft.Data.Sqlite.Core | 8.0.11 | SQLite ADO.NET 提供程序 |
| Microsoft.Extensions.Configuration | 8.0.0 | 配置框架 |
| Microsoft.Extensions.Configuration.Json | 8.0.1 | JSON 配置提供程序 |
| Microsoft.Extensions.Configuration.EnvironmentVariables | 8.0.0 | 环境变量配置 |
| Microsoft.Extensions.Http | 8.0.1 | HTTP 客户端工厂 |
| Microsoft.Extensions.DependencyInjection | 8.0.1 | 依赖注入容器 |
| Microsoft.Extensions.Logging.Console | 8.0.1 | 控制台日志提供程序 |
| Microsoft.Extensions.Logging.Abstractions | 8.0.2 | 日志抽象 |

## 环境要求

### 必需

- **.NET SDK**：8.0.100 或更高版本
- **Turso 账户**：在 [turso.tech](https://turso.tech) 注册免费账户
- **Turso CLI**：用于数据库管理（可选但推荐）

### 验证安装

```bash
# 检查 .NET 版本
dotnet --version

# 检查 Turso CLI（如已安装）
turso --version
```

## 项目结构

```
ToursDb/
├── Readme.md                    # 英文版文档
├── Readme-ZhCn.md              # 中文版文档（本文件）
├── ToursDb.slnx                # 解决方案文件
├── ToursDb.App/                # 主应用程序项目
│   ├── Program.cs              # 应用程序入口点
│   ├── ToursDb.App.csproj      # 项目文件
│   ├── appsettings.json        # 配置文件
│   ├── Data/
│   │   ├── ToursDbContext.cs                    # EF Core DbContext
│   │   └── TursoDbContextOptionsExtensions.cs   # EF Core 配置扩展
│   └── Models/
│       └── Tour.cs             # Tour 实体模型
└── ToursDb.Data/               # 自定义 Turso 数据提供程序库
    ├── ToursDb.Data.csproj     # 项目文件
    ├── TursoHttpClient.cs      # Turso API HTTP 客户端
    ├── TursoConnection.cs      # ADO.NET DbConnection 实现
    ├── TursoCommand.cs         # ADO.NET DbCommand 实现
    ├── TursoDataReader.cs      # ADO.NET DbDataReader 实现
    ├── TursoTransaction.cs     # ADO.NET DbTransaction 实现
    ├── TursoDbParameter.cs     # ADO.NET DbParameter 实现
    ├── TursoParameterCollection.cs  # ADO.NET DbParameterCollection
    ├── TursoSqliteConnection.cs     # 用于 EF Core 的 SqliteConnection 子类
    └── TursoSqliteCommand.cs        # 用于 EF Core 的 SqliteCommand 子类
```

## 快速开始

### 1. 克隆仓库

```bash
git clone https://github.com/yourusername/ToursDb.git
cd ToursDb
```

### 2. 创建 Turso 数据库

```bash
# 安装 Turso CLI（如未安装）
# Windows: 使用 winget 或从官网下载
winget install Turso.TursoCLI

# macOS/Linux:
curl -sSfL https://get.tur.so/install.sh | bash

# 登录 Turso
turso auth login

# 创建新数据库
turso db create my-tours-db

# 获取数据库 URL
turso db show my-tours-db --http-url

# 生成认证令牌
turso db tokens create my-tours-db
```

### 3. 配置应用程序

更新 `ToursDb.App/appsettings.json`：

```json
{
  "Turso": {
    "DatabaseUrl": "https://your-database-url.turso.io",
    "PipelineVersion": "v2"
  }
}
```

设置认证令牌为环境变量：

**Windows (PowerShell)：**
```powershell
$env:TURSO_AUTH_TOKEN = "your-auth-token-here"
```

**Windows (CMD)：**
```cmd
set TURSO_AUTH_TOKEN=your-auth-token-here
```

**macOS/Linux：**
```bash
export TURSO_AUTH_TOKEN="your-auth-token-here"
```

### 4. 构建项目

```bash
# 还原 NuGet 包
dotnet restore

# 构建解决方案
dotnet build
```

### 5. 运行应用程序

```bash
# 运行应用程序
dotnet run --project ToursDb.App
```

### 预期输出

```
=== ToursDb - EF Core + Turso (HTTP API) ===

Ensuring database schema...
Schema ready.

--- CREATE: Adding tours ---
Created 4 tours.

--- READ: All tours ---
[1] Paris City Break | Paris, France | $1299.99 | 5 days | Active: True | Created: 2024-01-15
[2] Tokyo Adventure | Tokyo, Japan | $2499.50 | 10 days | Active: True | Created: 2024-01-15
...

--- READ: Active tours under $2000 ---
[1] Paris City Break | Paris, France | $1299.99 | 5 days | Active: True | Created: 2024-01-15
...

--- UPDATE: Modifying a tour ---
Updated tour [1] - New price: $1499.99
Verified: [1] Paris City Break | Paris, France | $1499.99 | 5 days | Active: True | Created: 2024-01-15

--- DELETE: Removing a tour ---
Deleted tour [4] Northern Lights Iceland

--- FINAL STATE: Remaining tours ---
...

Total tours in database: 3

=== Demo complete! ===
```

## 配置说明

### 环境变量

| 变量 | 必需 | 描述 |
|----------|----------|-------------|
| `TURSO_AUTH_TOKEN` | 是 | Turso 数据库认证令牌 |
| `TURSO_DATABASE_URL` | 否 | 覆盖 appsettings.json 中的数据库 URL |

### appsettings.json

```json
{
  "Turso": {
    "DatabaseUrl": "https://your-database-url.turso.io",
    "PipelineVersion": "v2"
  }
}
```

### 配置优先级

1. 环境变量（最高优先级）
2. `appsettings.json`
3. 默认值（最低优先级）

## 开发规范

### 代码风格

- 遵循 [C# 编码约定](https://docs.microsoft.com/zh-cn/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- 使用可空引用类型（项目中已启用）
- 使用隐式 using（项目中已启用）

### 构建

```bash
# 调试构建
dotnet build

# 发布构建
dotnet build -c Release
```

### 运行测试

```bash
# 运行所有测试
dotnet test

# 详细输出运行测试
dotnet test --verbosity normal
```

### 添加新迁移

```bash
# 添加新迁移
dotnet ef migrations add InitialCreate --project ToursDb.App

# 应用迁移
dotnet ef database update --project ToursDb.App
```

## 常见问题排查

### 常见问题

#### 1. 缺少认证令牌

**错误：**
```
ERROR: Turso database URL and auth token are required.
```

**解决方案：**
设置 `TURSO_AUTH_TOKEN` 环境变量：
```bash
export TURSO_AUTH_TOKEN="your-token-here"
```

#### 2. 数据库连接失败

**错误：**
```
Turso API error: 401 Unauthorized
```

**解决方案：**
- 验证认证令牌是否有效
- 检查数据库 URL 是否正确
- 确保数据库存在：`turso db list`

#### 3. 构建错误

**错误：**
```
error NETSDK1004: Assets file not found
```

**解决方案：**
还原 NuGet 包：
```bash
dotnet restore
```

#### 4. EF Core 日志

应用程序已配置详细 EF Core 日志：
```csharp
optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
optionsBuilder.EnableSensitiveDataLogging();
```

这会将所有 SQL 语句输出到控制台。

### 获取帮助

- [Turso 文档](https://docs.turso.tech)
- [EF Core 文档](https://docs.microsoft.com/zh-cn/ef/core/)
- [ADO.NET 文档](https://docs.microsoft.com/zh-cn/dotnet/framework/data/adonet/)

## 许可证

本项目基于 MIT 许可证。详见 [LICENSE](LICENSE) 文件。

---

**注意**：这是一个演示项目。如需用于生产环境，建议添加：
- 适当的错误处理和重试逻辑
- 连接池
- 健康检查
- 结构化日志（如 Serilog）
- 单元测试和集成测试
