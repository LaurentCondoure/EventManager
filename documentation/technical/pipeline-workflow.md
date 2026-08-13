# Pipeline Workflow

## Why the CD pipeline is manually triggered

Azure DevOps YAML pipeline resources support OR logic only: `trigger: true` on two pipeline
resources means the CD runs when **either** completes, not when **both** do. There is no
native AND gate for multiple pipeline resource triggers in YAML pipelines.

A deployment to production requires that **both** the API and the frontend artifacts are green
before applying. The safe, explicit solution is a manual trigger: the operator triggers the CD
pipeline after confirming both CI pipelines have succeeded.

---

## Normal deployment flow

```
1. Developer pushes to main
        │
        ├─ touches backend/** ──→ [backend CI] Build → Test → Publish ──→ artifact: api
        │
        └─ touches frontend/** ──→ [frontend CI] Lint → Test → Build ──→ artifact: ui

2. Operator verifies both CI pipelines are green on main

3. Operator triggers [CD pipeline] manually in Azure DevOps
        │
        ├─ Stage: Infrastructure (terraform apply)
        │     └─ Manual approval gate (production environment)
        │           └─ Approver reviews terraform plan, clicks Approve
        │
        ├─ Stage: Deploy API ────────────────────────────────────┐
        │     Downloads artifact: api                            │ parallel
        │     Deploys to App Service                             │
        │                                                        │
        └─ Stage: Deploy Frontend ───────────────────────────────┘
              Downloads artifact: ui
              Deploys to Static Web App
```

---

## Step-by-step operator checklist

1. Verify backend CI is green on `main`:
   *Pipelines → azure-pipelines-backend → last run on main → succeeded*

2. Verify frontend CI is green on `main`:
   *Pipelines → azure-pipelines-frontend → last run on main → succeeded*

3. Trigger the CD pipeline:
   *Pipelines → azure-pipelines-cd → Run pipeline → Branch: main → Run*

4. Wait for the Infrastructure stage to pause for approval.

5. Review what terraform will change (refer to the `terraform-plan` artifact from the
   latest terraform CI run if needed).

6. Approve in the `production` environment:
   *Pipelines → Environments → production → pending approval → Approve*

7. Wait for Deploy API and Deploy Frontend to complete.

8. Verify the deployment:
   - API: `GET https://<app-service-name>.azurewebsites.net/api/events`
   - Frontend: open the Static Web App URL

---

## What the CD pipeline downloads

The CD pipeline declares both CI pipelines as resources. When triggered, it downloads
the artifact from the **latest successful run** of each pipeline on `main`:

| Artifact | Source pipeline | Downloaded by |
|---|---|---|
| `api` | azure-pipelines-backend (Publish stage) | Deploy API stage |
| `ui` | azure-pipelines-frontend (Build stage) | Deploy Frontend stage |

The operator's responsibility (step 1 and 2 above) is to ensure those latest runs are
both green before triggering the CD — this is the AND guarantee that the pipeline
trigger mechanism cannot enforce natively.

---

## Approval gate setup

The Infrastructure stage targets the `production` environment. Azure DevOps pauses the
pipeline and notifies the configured approvers before running `terraform apply`.

To configure approvers:
*Azure DevOps → Environments → production → Approvals and checks → Add → Approvals*

No infrastructure change is applied without explicit human review of what terraform will do.
