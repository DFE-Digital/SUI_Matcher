# Client-Agent Stack

This stack root represents the current deployable infrastructure shape.

It composes:

- shared Azure modules for identity, registry, observability, secrets, and the container apps environment
- client-agent-specific VM, firewall, routing, and log collection resources

The authoritative topology root for this architecture is `infra/stacks/client-agent/main.bicep`, and `.github/workflows/gh-client-infra-deploy.yml` deploys this root directly.

The legacy entry points under `src/app-host/infra` and `src/SUI.Client/SUI.Client.Watcher/infra` remain in place for compatibility with existing deployment and developer workflows.
