# CoreSecureBoilerPlate

# Secure Enterprise .NET 10 Boilerplate

A production-ready, highly secure, and observable boilerplate built on top of the **ASP.NET Core 10** ecosystem. This template is architected using **Clean Architecture (Onion Architecture)** patterns alongside **CQRS via MediatR**, specifically designed to satisfy the rigorous security and compliance standards required by medium-to-large scale financial and enterprise applications.

---

## Architectural Overview

The solution enforces a strict separation of concerns, decoupling domain logic from external infrastructure, web representations, and database frameworks.



* **Domain:** Contains pure enterprise business objects, specifications, entities, and core domain exceptions.
* **Application:** Orchestrates business workflows using the CQRS pattern via MediatR, handles data DTO mapping, and drives declarative input verification via FluentValidation pipeline behaviors.
* **Infrastructure:** Encapsulates external technical concerns such as EF Core database mappings, JWT token mechanics, and transactional security cryptography.
* **Web API:** The delivery tier leveraging minimal API endpoints protected by specialized pipelines and secure HTTP middleware configurations.

---

## Security & Hardening Features

This boilerplate mitigates top OWASP vulnerabilities out-of-the-box through layered defensive controls:

### 1. Authentication & Session Management
* **ASP.NET Core Identity Core:** Configured with robust cryptographic hashing standards.
* **Account Lockout Policy:** Automatically flags and locks identities for 15 minutes after 5 consecutive failed access attempts to neutralize brute-force spraying.
* **Dual-Token Rotation System:** High-entropy JWT Access Tokens paired with secure `HttpOnly`, `SameSite=Strict`, and `Secure` cross-domain Refresh Tokens.
* **Multi-Factor Authentication (MFA/2FA):** Integrated via dynamic, verified HTML-templated OTP workflows.

### 2. Fine-Grained Authorization
* **Role-Based Access Control (RBAC):** Native mapping of user identity claims to protected minimal endpoints using atomic builders (e.g., restricted administrative utilities like `AdjustPrice`).

### 3. Traffic Filtering & Network Perimeter
* **Strict CORS Configurations:** Isolated context boundaries separating explicit development origins (`localhost`) from production-grade domain white-lists.
* **Rate Limiting Policies:** Granular request windows utilizing fixed-window partitions to shield public authentication pipelines (`LoginPolicy`) and processing endpoints (`ProductPolicy`) from volumetric denial-of-service attempts.
* **Host Header Injection Defense:** Strict binding parameters utilizing explicit `AllowedHosts` filtering definitions.

### 4. Client-Side Browser Hardening
Custom HTTP middleware actively injects standard security headers into every outbound response stream:
* `X-Frame-Options: DENY` (Anti-Clickjacking defense)
* `X-Content-Type-Options: nosniff` (Anti-MIME Sniffing protection)
* `X-XSS-Protection: 1; mode=block` (Browser-side script filtering)
* `Strict-Transport-Security (HSTS)` (Enforces strict HTTPS communication)

---

## Observability & Resiliency Tier

### Structured Logging & Distributed Tracing
* Powered by **Serilog** configured to output structured JSON data streams.
* **Correlation ID Pipeline:** Custom HTTP middleware captures or generates an `X-Correlation-ID` header for every incoming request. This token travels across the MediatR pipelines and database boundaries, enabling seamless distributed tracing across logs.
* **Performance Monitoring:** Built-in MediatR behaviors monitor query execution timings, flagging slow-performing request thresholds (>500ms) with warning alerts in production environments.

### Centralized Exception Mitigation
* Implements the native C# 10 **`IExceptionHandler`** pattern to capture runtime failures seamlessly.
* Eradicates code leak exposures (Information Disclosure) by intercepting infrastructure errors and serializing them into standard **RFC 7807 (Problem Details)** JSON contracts, appending the tracking `correlationId` automatically.

### Automated Infrastructure Diagnostics
* Exposes dedicated, lightweight diagnostic endpoints for container orchestrators (e.g., Kubernetes probes):
    * `GET /health/live`: Fast application process validation.
    * `GET /health/ready`: Deep-tier network connectivity and SQL Server readiness validations.

---

## Configuration & Environment Splitting

The boilerplate maintains a strict boundary between operational runtime environments:
* **`appsettings.Development.json`:** Formatted for local debugging cycles, enabling interactive documentation structures (**Scalar / OpenApi**), explicit detailed runtime tracing logs, and local SQL Express connections.
* **`appsettings.Production.json`:** Engineered for secure hosting nodes, enforcing strict log verbosity overrides (`Warning`/`Error`), short-lived access tokens (5 minutes), forced TLS connection strings, and restricted documentation access.

---

## Getting Started

### Prerequisites
* .NET 10 SDK
* SQL Server (LocalDB / SQLEXPRESS)

### Installation
1. Clone the repository:
   ```bash
   git clone [https://github.com/your-username/your-repo-name.git](https://github.com/your-username/your-repo-name.git)
   cd your-repo-name
