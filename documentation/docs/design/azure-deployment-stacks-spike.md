# SUI-1838: Azure Deployment Stacks Spike

**Date:** `2026-06-26`

**Status:** Findings and recommendation

**Scope:** Whether SUI Matcher should use Azure Deployment Stacks for the Bicep infrastructure under `infra/stacks`.

This note is about the Azure Resource Manager feature named **Deployment Stacks**. It is separate from the repo's existing use of "stack" to mean an architecture-specific deployment root such as `client-agent`, `blob-event-processor`, or `api-batch-processor`.

## Summary

Azure Deployment Stacks would be useful for SUI Matcher, particularly for tracking and selectively removing SUI-owned resources from a client-owned resource group. However, given the nature of this project and the current delivery timelines, they are unlikely to add enough value to justify the implementation and operational effort now.

The feature provides managed resource tracking, controlled cleanup, and optional deny settings. Those are real benefits, but this project does not create and tear down environments frequently. Most infrastructure lifecycle operations are occasional, so the value of automating selective cleanup is correspondingly limited.

Adoption would also need to cater safely for two materially different deployment shapes:

- SUI creates and owns the resource group in the SUI Azure tenant, where deleting the resource group remains a simple and effective teardown
- SUI deploys into an existing resource group in a client tenant, where client-owned resources may coexist with SUI-owned resources and teardown must be selective

Supporting both shapes would require workflow changes, ownership rules, safeguards, testing, and operator guidance. The main trade-offs are:

- for stack-owned environments, the current `infra/stacks` model already gives each architecture a deterministic resource group boundary
- teardown is resource-group based only for stack-owned environments; it is not suitable where SUI resources live alongside client-owned resources
- existing workflows rely on `az deployment sub what-if`, but Azure Deployment Stacks do not currently support what-if
- `blob-event-processor` supports existing resource-group and existing storage-account modes, which means any deployment-stack adoption must separate SUI-owned resources from externally owned resources very carefully
- some Azure Deployment Stack protections do not cover implicit resources, Key Vault secrets, resource-group deletion, or moved resources
- the migration and ongoing operational complexity would be incurred now, while the selective teardown benefit would be used only occasionally

The recommendation is therefore not to adopt Azure Deployment Stacks within the current timelines. Keep normal subscription-scope Bicep deployments as the default, retain resource-group deletion for fully SUI-owned environments, and handle shared client resource groups through an explicit selective teardown process when required. Deployment Stacks should remain a credible future option if shared-resource-group deployments or selective teardowns become frequent enough to justify the investment.

## Current SUI Matcher Model

The current infrastructure structure is:

- `infra/stacks/client-agent/subscription.bicep`
- `infra/stacks/blob-event-processor/subscription.bicep`
- `infra/stacks/api-batch-processor/subscription.bicep`

Each subscription-scope wrapper creates or targets a resource group and then deploys the resource-group-scoped `main.bicep` for that architecture.

For stack-owned environments, the operational boundary is the whole resource group. Decommissioning means inspecting and then deleting the stack-owned resource group through `.github/workflows/gh-stack-decommission.yml`.

That is not the only deployment shape. `blob-event-processor` can deploy into an existing resource group and can use an existing storage account:

- `resourceGroupMode=existing`
- `storageAccountMode=existing`

In that shape, the resource group can contain client-owned resources alongside the SUI-owned resources. We cannot safely use the current resource-group deletion model there. Teardown needs to become selective.

These are lifecycle operations rather than routine deployment operations. Environments are not currently being created and destroyed often enough for teardown automation alone to provide a strong return on the adoption effort.

The current deployment workflows also support a what-if-first path before deployment:

- `.github/workflows/gh-client-agent-infra-deploy.yml`
- `.github/workflows/gh-blob-event-processor-infra-deploy.yml`
- `.github/actions/stack-infra-run/action.yml`

For stack-owned environments, this is a simple model: Bicep defines the topology, `az deployment sub what-if` previews changes, `az deployment sub create` applies them, and resource-group deletion is the explicit teardown path.

## What Azure Deployment Stacks Add

Azure Deployment Stacks are ARM resources of type `Microsoft.Resources/deploymentStacks`. A deployment stack is created from a Bicep file or ARM template and records the Azure resources it manages.

The main added capabilities are:

- **Managed resource tracking:** Azure keeps a list of resources associated with the deployment stack.
- **Cleanup behavior:** when a resource is removed from the template, `actionOnUnmanage` can detach it or delete it.
- **Deny settings:** managed resources can be protected from delete or write/delete operations, with explicit exclusions for selected principals or actions.
- **Cross-scope management:** stacks can exist at resource group, subscription, or management group scope and deploy resources at appropriate child scopes.

These capabilities are most valuable when resource lifecycle is more granular than "delete the environment resource group", or where accidental manual changes/deletes are a recurring risk.

## Benefits For SUI Matcher

Azure Deployment Stacks could help in these cases:

- **Safer selective cleanup:** resources removed from Bicep could be deleted as part of a stack update instead of left behind.
- **Stronger drift resistance:** deny settings could prevent accidental resource deletes or writes outside the deployment path.
- **Clearer ownership inventory:** the deployment stack's managed resource list could show which resources are owned by a stack.
- **Potential future fit for shared environments:** if future environments contain shared resource groups that cannot be deleted wholesale, stack-managed cleanup could be useful.

For example, if `blob-event-processor` had to live in a client-owned resource group permanently, a deployment stack could potentially track the SUI-owned subset of resources more explicitly than today's deployment history and naming conventions.

This is the strongest argument for using Azure Deployment Stacks in SUI Matcher. They could give us a cleaner teardown model for the already-supported `resourceGroupMode=existing` case:

- deploy SUI-owned resources into the client-owned resource group through a deployment stack
- leave client-owned resources as `existing` references rather than stack-owned resources
- delete or detach only the resources that the deployment stack manages
- avoid deleting the containing resource group

That model has genuine value, but the project currently expects to use it only occasionally. It is not enough on its own to justify changing the deployment model during the current delivery period.

## Costs And Risks

The current costs are more important for this repo than the benefits.

### Limited Operational Payback

Deployment Stacks are most compelling when environments or individual managed resources are regularly created, changed, and removed, or when manual drift is a recurring operational problem.

That is not the current SUI Matcher usage pattern. Deployments are ongoing, but full environment creation and teardown are occasional. In SUI-owned environments, resource-group deletion already provides a clear teardown boundary. In client-owned environments, selective teardown is harder, but it is not currently frequent enough to justify reworking every deployment path around it.

The implementation cost would include designing, testing, and maintaining the feature across both ownership models before the team receives much recurring benefit.

### No What-If Support

Microsoft currently lists what-if as unsupported for deployment stacks.

This is a material downside. The repo's deployment workflow is intentionally what-if-first, especially because infrastructure changes include networking, private endpoints, DNS, role assignments, storage, Event Grid, Container Apps, and monitoring.

Replacing `az deployment sub what-if` with deployment stack update behavior would reduce deployment review quality unless we built another preview process around plain Bicep deployments. That would weaken the main reason to use deployment stacks in the first place.

There are mitigations, but no exact replacement:

- run `az stack sub validate` before create/update to validate the deployment stack request, `actionOnUnmanage`, deny settings, template, and parameters
- run ordinary `az deployment sub what-if` against the same Bicep file and parameters to preview the ARM resource changes
- inspect the current deployment stack managed-resource list with `az stack sub show` before any update or teardown
- compare the managed-resource list with an independently generated inventory, such as expected names, tags, and `az resource list` output for the target resource group
- use `actionOnUnmanage=detachAll` for first deployments and updates until the managed-resource list is trusted
- require a separate confirmation step before any `deleteResources` teardown

This would give us most of the deployment preview we have today, but it still would not preview Deployment Stack-specific behavior. In particular, ordinary ARM what-if would not show which previously managed resources will be detached or deleted by `actionOnUnmanage`, would not validate the safety of the managed-resource list, and would not fully model deny assignment side effects.

A safe pilot should therefore treat `az deployment sub what-if` as a template-change preview, not as a Deployment Stack teardown preview.

### Resource Group Teardown Is Optional, Not Required

Microsoft documents that deleting a resource group bypasses deny assignments for deployment stacks created at resource-group scope, because the parent resource group is not managed by that stack.

The workaround is to deploy from subscription scope and include the resource group in the template. Our current subscription-scope wrappers already create the stack-owned resource group, so this is possible in principle.

For fully stack-owned environments, this overlaps heavily with the current subscription-scope wrapper plus manual resource-group decommission workflow.

For client-owned resource groups, the conclusion is different. The useful design would be a deployment stack that manages the SUI-owned resources inside the existing resource group, while leaving the resource group and client-owned dependencies outside the stack's delete scope. That could be better than either manual selective deletion or no teardown support.

### Existing Resource Modes Do Not Match Full Ownership

`blob-event-processor` can deploy with:

- `resourceGroupMode=existing`
- `storageAccountMode=existing`

Those modes are useful because some environments may need to use client-provided resources or resource groups. They also make Azure Deployment Stacks more complicated, because the stack must not treat externally owned resources as disposable.

The intended shape would need to be explicit:

- client-owned resource group: referenced or targeted, not deleted
- existing storage account: referenced as `existing`, not deleted
- SUI-created resources: eligible for managed cleanup where Azure Deployment Stacks can track and delete them safely
- non-deletable or externally owned resources: detached rather than deleted

This is possible in principle, but it needs a careful proof of concept. We need to confirm exactly which deployed child resources are tracked and removable by the deployment stack, especially storage containers, queues, Key Vault-related resources, private endpoints, DNS links, role assignments, and Event Grid resources.

### Protection Is Not Complete

Deployment stack deny settings are not a general lock for everything related to the environment:

- implicitly created resources are not managed by the stack
- Key Vault secrets cannot be deleted by deployment stacks
- moved resources can lose stack protection
- deny assignments are not supported at management group scope, although a management-group stack can deploy to subscription scope
- deny settings apply to control-plane operations, not data-plane operations

This matters for SUI Matcher because several important resources have child resources, private endpoints, secrets, role assignments, and data-plane behavior. Deployment stacks would not remove the need for normal review, runbooks, least-privilege CI/CD identities, and explicit teardown checks.

### Operational Complexity

Adopting deployment stacks would require changes beyond swapping one CLI command:

- change workflows from `az deployment sub create` to `az stack sub create`
- decide `actionOnUnmanage` separately for fully owned, existing-resource, and decommission flows
- decide whether to use `denyDelete` or `denyWriteAndDelete`
- manage deny-setting exclusions for CI/CD and break-glass identities
- handle stack-out-of-sync errors safely
- train operators on detach vs delete semantics
- update direct deployment guidance for environments where GitHub Actions is not the deployment route

That extra surface area is only worth it if we need Azure-managed cleanup or deny protection enough to compensate for losing native stack what-if.

## Option Comparison

| Option | Pros | Cons | Fit |
| --- | --- | --- | --- |
| Keep normal Bicep deployments | Keeps current what-if-first workflow; simple resource-group ownership; lowest change | No Azure-managed resource tracking; selective teardown stays manual in client-owned resource groups | Best default now |
| Adopt deployment stacks for all `infra/stacks` deployments | Managed resource list; delete/detach semantics; deny settings | No what-if; more destructive footguns; needs workflow and runbook changes; awkward with existing-resource modes | Not recommended |
| Pilot deployment stacks for one fully owned non-production stack | Tests feature with limited blast radius; validates CLI, permissions, deny settings, and delete behavior | Not representative of the client-owned resource-group problem; limited operational benefit | Low priority |
| Pilot deployment stacks for a client-owned resource group scenario | Tests the main potential benefit: selective ownership and teardown without deleting the resource group | Needs careful delete/detach guardrails; still lacks what-if; must prove externally owned resources are not managed | Best pilot if the need increases |
| Use deployment stacks only for protection, with `detachAll` | Deny settings without managed delete behavior | Still loses what-if if used as deployment path; cleanup benefit mostly unused | Weak fit |

## Recommendation

Keep the current normal Bicep deployment model as the default for SUI Matcher and do not invest in adopting Azure Deployment Stacks within the current project timelines.

For SUI-owned environments, continue using the resource group as the ownership and teardown boundary. For deployments into a client-owned resource group, use a deliberate selective teardown procedure that identifies and removes only SUI-owned resources. This is less automated than a Deployment Stack, but it avoids introducing a second deployment lifecycle while this remains an occasional operation.

This is a prioritisation decision rather than a conclusion that Deployment Stacks have no value. If shared client deployments or selective teardowns become a regular requirement, the first proof of concept should target the environment shape where the current model is weakest: SUI-owned resources deployed into a client-owned resource group that cannot be deleted wholesale.

That future pilot should include:

- a non-production client-owned or client-like resource group
- at least one externally owned dependency, such as an existing storage account, represented as an `existing` Bicep resource
- a known set of SUI-owned resources deployed by the stack
- `az stack sub validate` and ordinary `az deployment sub what-if` as required preflight checks
- managed-resource inventory capture before and after each stack operation
- `actionOnUnmanage=detachAll` for early trials
- a separate teardown trial using `deleteResources` only after the managed resource list has been inspected
- no deny settings at first, then a separate `denyDelete` trial if cleanup behavior is understood
- explicit verification that CI/CD and break-glass identities can still update and recover the environment
- a documented rollback path using stack deletion with detach behavior

It should not use `deleteAll`, `deleteResources`, or `denyWriteAndDelete` in the first deployment trial.

If the pilot proves that Azure tracks only the SUI-owned resources we expect, and that teardown can remove them without touching the resource group or client-owned resources, Deployment Stacks would be a good candidate for the shared-resource-group deployment path.

## Decision Criteria For Revisit

Reconsider Azure Deployment Stacks if one or more of these becomes true:

- environments can no longer be safely decommissioned by deleting stack-owned resource groups
- selective cleanup of removed resources becomes a recurring problem
- shared client resource groups become a common deployment target
- environment creation and teardown become frequent operational tasks
- accidental manual deletion/write drift becomes a recurring production risk
- Azure adds what-if support for deployment stacks
- existing-resource modes become a standard deployment path rather than an exception
- the team needs a formal Azure-managed ownership inventory in addition to naming and resource-group boundaries

Until then, the current approach is clearer and safer for this repo.

## Sources

- [Create and deploy Azure deployment stacks in Bicep](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/deployment-stacks), Microsoft Learn, updated 2026-06-24
- [Known issues for deployment stacks](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/deployment-stacks-known-issues), Microsoft Learn, updated 2026-06-24
