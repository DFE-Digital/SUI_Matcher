# Client-Agent Stack

This stack root represents the full DfE-hosted test deployment shape.

It composes:

- shared Azure modules for identity, registry, observability, secrets, and the container apps environment
- client-agent-specific VM, firewall, routing, and log collection resources

The authoritative topology root for this architecture is `infra/stacks/client-agent/main.bicep`.

`.github/workflows/gh-client-agent-infra-deploy.yml` deploys this root. The existing `src/app-host/infra` workflow remains in place via `.github/workflows/gh-deploy-infra.yml`, while the current laptop-driven deployment flow for existing client environments remains documented under `src/app-host/infra/deploy_readme.md`.
