using AsiBackbone.AspNetCore.Endpoints;
using Microsoft.AspNetCore.Mvc;

namespace AsiBackbone.Samples.PlainAspNetCoreHost;

[ApiController]
[Route("sample/ergonomic/controller-public")]
public sealed class SampleGovernanceController : ControllerBase
{
    [HttpPost]
    [RequireGovernancePolicy(typeof(SampleControllerEndpointPolicy))]
    [RequireLiabilityHandshake]
    [RequireCapabilityGrant("sample.high-risk.execute")]
    [EmitGovernanceAudit]
    public IActionResult ExecuteHighRiskAction()
    {
        return Ok(new
        {
            message = "Public controller action executed after AsiBackbone endpoint governance metadata was evaluated."
        });
    }
}

public sealed class SampleControllerEndpointPolicy
{
}
