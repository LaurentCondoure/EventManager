# AI Usage — Concrete usage

## Context

### Setting up

This project (EventManager) was built as part of an intensive training to improve skills in caching, full Text Search, Infrastructure as Code and Cloud development.
The technical stack was chosen to focus on those principle (mongoDb, Redis, ElastichSearch, Terraform), base stack (c#, .net 8, vue.js) was chosen to give me a known basis to start. And the functional perimeter was defined according to my training needs and specified before any code was written.

So the specifications, the architecture, technology selection, and data model were defined upfront and remained owned throughout.

### AI support decision

The AI is starting to be a necessary skill at it take a prominent place in development industry to gain productivity. Prompt is also important to master, in order to limit the token usage and keep working with this tool efficiently.

Claude code was chosen to be an assistant for four purposes : 
- Manage AI in a real level project (in complement of small exercices)
- Support the learning as trusted peer to discuss and confront point of view
- Generate code on my core stack, letting me review its code
- Review the grammar and lexical content of english documentation

---

## Project lifeCycle development

### Learning Principle / Peer challenge

**Stack:** NoSQL (MongoDB, Redis, Elasticsearch), xUnit, vite, vitest, Varnish, Terraform, Docker.

Without that grounding, I cannot neither make the right architectural choice, nor estimate work credibly on those topics. So I made the choice to process technology by technology to fully focus my practice on each. 
I first read and implemented functionalities my own one by one.
To do so, I established and followed this sequence : 
- Get a first look and learn how a technoly works
- Decide a first implementation base on my understanding
- Ask Claude to review it with only code and project documentation
- Confront the review with my own explanation
- Make the final choice and adapt the implementation through ADR

Claude Code was considered as a "trusted peer" and used as a sparring partner to engage early with technical depth — beyond what my training stage alone would naturally produce.

**Concrete example — Redis cache invalidation strategy**

In my initial Redis implementation, list caches were not properly invalidated on writes. When I addressed it, Claude proposed a wildcard delete (DEL events:list:page:*), which is the more common pattern in introductory documentation for cache invalidation on event creation.

Rather than accepting the proposal, I asked Claude to justify the choice and explain its implications. For a learning exercise, the wildcard approach would have been acceptable. As it not worth a production-grade standards, and at that bar the justification was not strong enough to commit to. I asked for alternative options and analyzed each against two criteria I held as priority: consistency under concurrent writes, and scalability of the invalidation cost.

I selected the versioned-key strategy: a single INCR events:list:version counter renders all existing page keys logically stale in O(1), without scanning the keyspace. Three properties drove the choice — atomicity (a single atomic operation, no race window), cost (O(1) regardless of keyspace size), and cluster compatibility (single-shard operation).
The decision and its rationale are recorded in ADR-006.

**What this mode demonstrates:**

**Personal:** Lead-level architectural judgment on technologies under active learning — the ability to hold a design position against external challenge, grounded in technical reasoning and stated criteria rather than authority.

**Structural:** AI has to be led in a solid and complete context to be an efficient partner. It requires remaining critical of its answers and trusting its knowledge, not its judgment.

### Leader Principle / Junior under specification

**Stack:** .NET 8, ASP.NET Core, Dapper, SQL Server, Vue.js, Azure DevOps.

On my core stack, my work was no longer about thinking through unknowns — design choices, patterns, trade-offs and architectural fit are already internalized. What remains is the writing of the code itself. In this mode, Claude Code operated as a junior developer under tight specification: I defined what to build and how, Claude generated the implementation, I reviewed every file as I would review a junior's work.

I defined the following workflow to complete the task : 
- I specified the design and the constraints
- Claude produced the code with approval before each edit
- I read each edit and corrected what was wrong, suboptimal, or misaligned with the project's standards
- I committed through Git myself to ensure no unreviewed or unintended modifications reached the GitHub repository

Claude was not a peer anymore in this mode. He was a fast junior with no project context unless given. The benefit was speed of execution, not depth of judgment — judgment stayed with me, where my expertise made it credible.

**Concrete example — CI/CD pipeline trigger architecture (Azure DevOps)**
The release pipeline needed to deploy only after two upstream CI builds completed successfully — backend CI and frontend CI. The intended trigger semantics were AND: deploy when both have succeeded, not when either has.
Claude initially generated a CD pipeline with trigger: true declared on both CI resources. This produces OR semantics in Azure DevOps YAML: the deployment fires as soon as either CI completes, regardless of the other. The configuration looked correct syntactically; the behavior was wrong by design.
I caught the gap on review. Azure DevOps YAML has no native AND gate between pipeline resources, which meant the generated configuration could not deliver the intended behavior under any reasonable variation. The architectural choice was not between fixing the YAML and refining it — it was between two strategies:

Script a synthetic AND gate (custom logic in the CD pipeline checking both upstream completions before proceeding) — fragile, hard to maintain, hides intent.
Switch to a manual trigger as the deployment gate — explicit, controllable, traceable, and preserves the engineering team's visibility on the release condition.

I chose the manual trigger approach and documented it as a deliberate trade-off: lose automation on the deployment trigger to gain correctness and human-controlled release semantics.
The decision was not about Claude generating bad code. The code Claude generated was syntactically valid and runnable. The decision was about recognizing that the architectural intent — when to release — could not be expressed with the construct Claude defaulted to, and choosing a different architecture that aligned intent and implementation honestly.

**What this mode demonstrates:**

**Personal:** Lead-level architectural review on mastered stack — the ability to recognize when generated code, however correct in form, fails to meet the design intent, and to redirect the architecture rather than patch the symptom.
**Structural:** AI generation operates at the level of patterns it has seen, not at the level of intent it cannot infer. The reviewer's role is to enforce the alignment between intent and implementation — a responsibility that does not transfer to the tool, regardless of the tool's quality.


### Beyond the modes
The two modes describe how Claude participates in the work. What follows describes what stays with me regardless of mode.
#### Intent ownership
Architecture, technology selection, data model, ADRs — defined by me, kept by me throughout. Claude operates inside that frame, it does not define it. When a mode 2 specification is handed off, the underlying decisions (why Dapper rather than EF Core, why versioned cache keys, why MongoDB for the reviews aggregate) have already been made and recorded. Claude follows those decisions; it does not rework them during implementation.

The same applies to documentation. Claude does not produce a document from a blank page on my behalf — the template is defined, the substantive content is written, and Claude works on top of that base: rewording, tightening, lifting the English from professional to fully professional. The dosage is the same as for code. Treating documentation as exempt from that discipline produces the same class of drift.

#### Gap tracking
A separate document — CLOSURE.md, together with the What changes now sections in the ADRs — records the distance between intent and what is actually delivered: what runs locally, what is not yet deployed to Azure, the rationale per component, the resolution path. Claude can write a gap entry when asked, but it cannot find them on its own. Spotting a gap requires holding the original intent in mind and recognising where delivery fell short of it. That awareness lives with me, not with the tool.

#### Discipline via memory.md
Over extended use, Claude Code drifts away from its own working rules — skipping options and jumping to a solution, burning tokens in loops, restating context that was already established. A memory.md file at the repository root captures each recurring drift as a rule. When Claude deviates, I invoke the rule by name rather than re-arguing the principle. The cycle is: observe the behaviour, write the rule, enforce it on the next violation. It is engineering management applied to a tool, not engineering of the tool itself.


