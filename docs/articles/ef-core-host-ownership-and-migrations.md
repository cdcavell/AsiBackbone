# EF Core Host Ownership and Migration Guidance

`AsiBackbone.EntityFrameworkCore` contributes model configuration and persistence helpers for ASI Backbone accountability records. The host application owns the `DbContext`, database provider, connection string, migrations, schema deployment, and operational database lifecycle.

> [!IMPORTANT]
> ASI Backbone does not take over