## Getting Started
The Application project (AppCore) coordinates the product’s use cases. It defines what the application can do and orchestrates the work required to complete each user action.

It may define interfaces for services or data access that it needs, but it should not depend on WPF, a specific database, or a specific device vendor. Use cases in this project should be testable without starting the UI.

Typical contents: use cases, application services, commands or request handlers, validation, and interfaces for external services.



## Responsibility
Use case, Bussiness logic, and Application services.

## Dependencies
Domain
