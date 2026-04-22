# Deployment IaC Stack Strategy

**Date:** `2026-04-02`

**Scope:** Proposed direction for restructuring deployment IaC so different local-authority architectures can be deployed without breaking the current deployment path.

This document captures the current proposed direction for deployment infrastructure in the SUI Matcher repo.

It is a design note intended to:

- formalise the current direction of travel
- record the main structural changes needed in the existing IaC
- make assumptions and follow-up work explicit

---

## 1. Overview

The current deployment IaC is not structured to support multiple deployment architectures cleanly.

Today the repo contains:

- a saved Aspire-shaped deployment baseline under `src/app-host/infra`
- an Aspire app definition in `src/app-host/azure.yaml` that assumes a single container app deployment flow
- additional client, VM, firewall, and connectivity infrastructure under `src/SUI.Client/SUI.Client.Watcher/infra`

This has been workable for the current deployment shape, but it is not a good fit for the next set of environments.

The next deployment target needs a different architecture from the current VM/client shape. A future deployment is also expected to diverge further, with batch-oriented Eclipse integration as the current baseline. If we continue extending the existing single-root deployment model with more flags and conditionals, the IaC will get harder to reason about, harder to validate, and more likely to break the current deployment path.

The proposed direction is to restructure deployment IaC around **separate deployment stacks by architecture pattern**, while keeping genuinely shared Azure resources in reusable modules.

---

## 2. Current Position

The current position in the repo is:

- Aspire is useful for local development, but it is not the authoritative deployment model
- deployment Bicep is manually maintained and already diverged from generated Aspire output
- the existing deployment structure effectively assumes one overall shape with environment-specific parameters
- the current VM/client infrastructure is already separate in practice, but not yet represented as a first-class stack boundary
- several networking and security steps are still partly manual

This creates two main problems:

1. architecture-specific infrastructure is mixed into a deployment model that looks more generic than it really is
2. adding a new architecture risks bolting it onto the current VM/client-oriented code rather than introducing a clear new boundary

---

## 3. Proposed Direction

The proposed direction is:

- keep Aspire focused on local development only
- stop treating `src/app-host/infra/main.bicep` as the universal deployment root for all environments
- restructure deployment IaC into separate top-level stacks based on architecture shape
- preserve the current deployment path behind its own explicit stack so it remains deployable
- add a separate `blob-event-processor` stack next
- shape the structure so `api-batch-processor` can follow without reworking the same boundaries again
- extract only genuinely shared Azure resources into reusable modules
- automate as much infrastructure as permissions allow, and explicitly document anything that must remain manual

The initial restructure should stay in this repo to avoid delaying delivery with a repo move. A later split to a private infra repo can be reconsidered once the new stack boundaries are stable.

---

## 4. Stack Model

The stack names and resource naming should stay **generic** and avoid local-authority names in code, stack names, workflows, and deployment artefacts.

Examples of generic stack labels:

- `client-agent`
- `blob-event-processor`
- `api-batch-processor`

These map to the current known deployment shapes as follows.

Each stack root should deploy an isolated environment by composing the shared Bicep modules it needs directly. Shared resources are therefore a module concern, not a separately deployed stack that other stacks depend on.

### 4.1 Client agent stack

Represents the current client-agent deployment shape:

- any shared Azure resources required by this architecture
- externally hosted client agent and related networking
- firewall and routing components required for the current model
- private connectivity needed by the client-agent path

This stack exists to keep the current infrastructure deployable during and after the refactor.

### 4.2 Blob event processor stack

Represents the next blob-triggered event processing deployment shape:

- any shared Azure resources required by this architecture
- blob storage and event-driven processing path
- eventing and function-hosted processing resources as required by that model
- network/security resources needed for that environment

This stack must not depend on the client-agent infrastructure.

### 4.3 API batch processor stack

Represents the current scheduled API batch baseline:

- any shared Azure resources required by this architecture
- scheduled batch processing against an external API
- API read/write integration concerns
- identity, secret, and networking requirements for that model

This should be treated as the baseline future path for batch-oriented API integration unless the architecture changes later.

---

## 5. Application Deployment Layer

The deployment stacks described above define environment topology and infrastructure ownership. They do not, by themselves, define where the application services are deployed from.

The intended ownership model is:

- `client-agent`, `blob-event-processor`, and `api-batch-processor` are the deployable infrastructure roots
- each stack root composes the shared modules it needs directly, alongside any architecture-specific infrastructure
- application deployment remains a separate layer that consumes outputs from the selected stack

In practical terms, the current application deployment artefacts under `src/app-host/infra` should be treated as application-layer deployment inputs, not as the authoritative source of environment topology.

This means:

- stack roots define what infrastructure exists
- shared modules define reusable infrastructure building blocks and stable outputs
- application deployment targets the outputs of the chosen stack rather than redefining the stack shape itself

---

## 6. Module Boundaries

Shared modules should be extracted from the current Bicep only where the resource is truly common.

Good candidates for shared modules:

- observability resources
- identities
- key vault
- shared ACA environment resources
- reusable networking/security building blocks
- stable deployment outputs consumed by application deployment

Poor candidates for shared modules:

- anything that exists only because one deployment architecture has a different topology
- pilot-specific VM, function, eventing, or Eclipse integration concerns
- modules that only stay "shared" by introducing many boolean switches

If a resource exists because one deployment architecture needs it, it should stay in that architecture's stack root.

---

## 7. Naming Rules

To keep public repository artefacts generic:

- avoid local-authority names in stack names, resource groups, workflow names, and parameter file names
- prefer names based on architecture pattern or integration shape
- keep environment/resource naming inputs generic and reusable

Examples:

- use `blob-event-processor` instead of an LA-specific name
- use `client-agent` instead of an LA-specific name
- use `api-batch-processor` instead of an LA-specific name

This rule applies to new deployment structure, new module names, and any new pipeline/workflow naming that follows later.

---

## 8. Implementation Order

The recommended order of work is:

1. extract shared modules from the current Bicep
2. recreate the current deployment path behind an explicit `client-agent` stack root
3. introduce the next `blob-event-processor` stack root and skeleton
4. define the future `api-batch-processor` stack contract and skeleton
5. update deployment documentation once the new structure is stable

This order keeps the current deployment path safe while making room for the next target architecture.

---

## 9. Success Criteria

The first restructure should be considered successful when all of the following are true:

- the current deployment path can be represented by an explicit `client-agent` stack root without changing its intended infrastructure shape
- the next `blob-event-processor` stack can be introduced without depending on `client-agent` resources
- the future `api-batch-processor` stack has a documented contract and placeholder structure, even if it is not fully implemented in the first pass
- shared modules expose the outputs needed by individual stack roots and the application deployment layer
- any current manual or externally owned deployment steps are either preserved and documented, or replaced with automated equivalents

Preserving deployability in this note means keeping the deployment path usable after the refactor, with equivalent infrastructure intent and clearly documented dependencies.

---

## 10. Confirmed Assumptions

The following assumptions are treated as current working direction:

- the next environment architecture to support is the `blob-event-processor` stack
- future Eclipse integration should be planned around the current batch model
- the current deployment path must remain deployable throughout the refactor
- Aspire should remain focused on local development rather than deployment modelling
- generic naming should be enforced for new stacks and resources
- the aim is to automate as much firewall, private endpoint, DNS, and routing work as permissions allow
- any infrastructure that cannot be automated yet should be recorded explicitly as manual or externally owned

---

## 11. Open Follow-Up Work

This note does not redesign the whole deployment process.

The following items remain follow-up work outside the first restructure:

- whether deployment IaC should later move into a private infra repo
- whether any later CI/CD changes should accompany the new stack structure
- the exact minimum first deployable slice for the next `blob-event-processor` application resources, where that belongs in separate implementation tickets
- the exact boundary between automatable networking/security resources and LA-owned manual steps in each tenant

---

## 12. Outcome Sought

At the end of this work, the repo should support:

- a deployable `client-agent` stack for the current deployment path
- a clean path to add `blob-event-processor` without bolting it into the current client-agent-oriented IaC
- a clear place for `api-batch-processor` to land later
- shared modules that are actually reusable
- deployment structure that reflects the real architecture differences between environments
