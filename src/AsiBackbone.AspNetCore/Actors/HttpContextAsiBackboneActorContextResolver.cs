using System.Security.Claims;
using AsiBackbone.Core.Actors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AsiBackbone.AspNetCore.Actors;

/// <summary>
/// Maps the current ASP.NET Core HTTP context into a framework-neutral AsiBackbone actor context.
///