var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.person_api>("person-api");

builder.Build().Run();
