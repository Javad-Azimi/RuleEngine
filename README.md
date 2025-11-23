# Tadbir Rule Engine

A powerful system for orchestrating API workflows with advanced mapping capabilities, built with ASP.NET Core 9 and Blazor Server.

## Features

- **Swagger Integration**: Import API definitions from OpenAPI/Swagger URLs
- **Rule Engine**: Create conditional logic for API orchestration using Microsoft RulesEngine
- **Mapping Engine**: Transform API outputs with flexible JSON templates and expressions
- **Policy Execution**: Execute multi-step API workflows with rules
- **Scheduling**: Run policies on cron schedules or on-demand
- **Execution Logging**: Complete audit trail of all executions
- **Blazor Admin UI**: Modern web interface for management

## Architecture

### Backend (TadbirRuleEngine.Api)
- ASP.NET Core 9 Web API
- Entity Framework Core with SQL Server
- Microsoft RulesEngine for conditional logic
- Custom Mapping Engine with template expressions
- Cron-based scheduling with Cronos
- Comprehensive logging with Serilog

### Frontend (TadbirRuleEngine.Web)
- Blazor Server application
- Bootstrap 5 UI components
- Real-time updates and notifications
- JSON editor with syntax highlighting
- Mapping preview and testing

## Getting Started

### Prerequisites
- .NET 9 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 or VS Code

### Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd TadbirRuleEngine
   ```

2. **Update connection string**
   Edit `TadbirRuleEngine.Api/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TadbirRuleEngineDb;Trusted_Connection=true;MultipleActiveResultSets=true"
     }
   }
   ```

3. **Run the backend API**
   ```bash
   cd TadbirRuleEngine.Api
   dotnet run
   ```
   API will be available at `https://localhost:7000`

4. **Run the frontend (in a new terminal)**
   ```bash
   cd TadbirRuleEngine.Web
   dotnet run
   ```
   Web UI will be available at `https://localhost:7001`

## Usage

### 1. Import Swagger Sources
- Navigate to "Swagger Sources"
- Add your OpenAPI/Swagger JSON URLs
- Click "Sync" to import API definitions

### 2. Create Policies
- Go to "Policies" and create a new policy
- Add rules to define your workflow logic

### 3. Configure Rules with Mapping
Example rule with mapping:
```json
{
  "type": "callApi",
  "apiId": 1201,
  "mapping": {
    "invoiceId": "{{previousResult.preInvoiceId}}",
    "customerName": "{{previousResult.customer.fullName}}",
    "totalAmount": "{{previousResult.amount}}",
    "amountAsString": "{{toString(previousResult.amount)}}",
    "processedAt": "{{dateNow()}}",
    "buyer": {
      "name": "{{previousResult.customer.fullName}}",
      "code": "{{previousResult.customer.code}}"
    }
  },
  "saveAs": "invoiceConversionResult"
}
```

### 4. Execute Policies
- Execute policies manually from the UI
- Set up cron schedules for automatic execution
- Monitor execution logs for results

## Mapping Engine

The Mapping Engine supports:

### Template Expressions
- `{{previousResult.fieldName}}` - Access previous API result
- `{{context.variableName}}` - Access execution context

### Built-in Functions
- `toString(value)` - Convert to string
- `toNumber(value)` - Convert to number  
- `concat(a,b,...)` - Concatenate values
- `dateNow()` - Current UTC timestamp
- `formatDate(date, format)` - Format date string
- `if(condition, trueValue, falseValue)` - Conditional logic

### Example Mapping
```json
{
  "salesInvoiceId": "{{previousResult.preInvoiceNumber}}",
  "items": "{{previousResult.items}}",
  "totalFormatted": "{{concat('$', toString(previousResult.amount))}}",
  "createdAt": "{{dateNow()}}",
  "buyer": {
    "name": "{{previousResult.customerName}}",
    "code": "{{previousResult.customerCode}}"
  }
}
```

## API Endpoints

### Swagger Sources
- `GET /api/swaggersources` - List all sources
- `POST /api/swaggersources` - Create new source
- `POST /api/swaggersources/{id}/sync` - Sync source

### Policies
- `GET /api/policies` - List all policies
- `POST /api/policies` - Create new policy
- `POST /api/policies/{id}/execute` - Execute policy

### Rules
- `GET /api/rules/by-policy/{policyId}` - Get rules for policy
- `POST /api/rules` - Create new rule

### Execution Logs
- `GET /api/executionlogs` - Get execution history

## Database Schema

The system uses Entity Framework Core with the following main entities:
- `SwaggerSource` - API source definitions
- `ApiDefinition` - Individual API endpoints
- `Policy` - Execution policies
- `Rule` - Individual rules with conditions and actions
- `ExecutionLog` - Execution history and results

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License.