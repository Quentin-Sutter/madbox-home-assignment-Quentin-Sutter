# Madbox Home Assignment – Quentin Sutter

## Description

As part of the recruitment process, I developed a focused gameplay prototype using the assets and constraints provided by Madbox.

The project implements a 3D hero controller inspired by *Archero*, featuring drag-based movement, auto-targeting, and a modular weapon system.

The objective was not to build a complete game, but to demonstrate my approach to gameplay architecture, system design, performance awareness, and game feel within a constrained production timeframe.

---

## Approach

For this assignment, I applied a workflow I previously experimented with on a personal project (outside Unity/C#), adapting it to this context.

I structured the project by first defining architectural guidelines and responsibilities for each system. I then used AI tools (ChatGPT and Codex) as productivity accelerators — primarily to generate structured drafts of systems based on precise prompts.

For each feature, my workflow was:

1. Define the system’s responsibility and constraints.
2. Craft and refine a detailed prompt describing architecture and expected behavior.
3. Generate an initial implementation draft.
4. Review, refactor, test, and adjust the result to align with performance, readability, and production standards.

I remained fully responsible for validating every design decision, ensuring system coherence, and integrating features into the overall architecture.

Once all core features were implemented and gameplay felt stable, I performed a final review pass across the codebase to simplify responsibilities, clarify ownership boundaries, and improve overall consistency.

---

## Time Breakdown

| Phase | Time |
|-------|------|
| Setup & Planning | ~30 min |
| Input System | ~40 min |
| State System Design | ~1h15 |
| Combat & Targeting | ~1h00 |
| Health System | ~20 min |
| Animation | ~1h00 |
| Weapon System | ~1h10 |
| Enemy Spawning | ~30 min |
| Weapon Switch | ~1h00 |
| Polish & Review | ~2h00 |
| README | ~1h00 |
| **Total** | **~9h45** |

Time was intentionally kept under the 10-hour limit.

---

## Difficulties

### 1. Choosing the Right Level of Abstraction

One of the main challenges was balancing abstraction and pragmatism.

I initially invested time designing a highly decoupled architecture, introducing multiple interfaces to enforce separation of concerns and potential extensibility. While this approach is valuable in larger productions, in the context of a constrained prototype it introduced unnecessary complexity and reduced readability.

The additional abstraction layers did not provide meaningful flexibility within the project scope and instead added indirection.

I refactored the system to simplify responsibilities, reduce unnecessary layers, and favor clarity over theoretical extensibility.

This reinforced an important lesson: good architecture is not about maximizing abstraction, but about choosing the appropriate level of complexity for the problem and its constraints.

---

### 2. Responsibility Boundaries

During the implementation of the core gameplay systems (state, targeting, combat), I initially allowed decision-making logic to spread across multiple scripts. Some components were making gameplay decisions when they should have only been responsible for execution.

This led to blurred responsibilities and made the system harder to reason about.

I refactored the architecture to centralize decision-making within the state controller, ensuring it acts as the single source of truth (“the brain”), while other systems (targeting, combat, animation) became reactive services.

This significantly improved clarity, maintainability, and debuggability.

---

### 3. Animation Generalization

Initially, I kept animation handling separate between hero and enemy for simplicity. I later attempted to unify both into a more generic animation handler to reduce duplication.

While the abstraction was conceptually valid, it did not provide meaningful value within the scope of this prototype. Future animation requirements and constraints are unknown, making early generalization speculative and potentially misaligned with real needs.

Although inheritance or additional abstraction layers could accommodate future variations, this approach risks introducing rigid hierarchies and unnecessary structural complexity.

In this context, maintaining simpler, purpose-driven animation handlers proved more appropriate.

---

## Architectural Improvements

### Animation Damage Timing

Damage timing is currently based on a hardcoded percentage of the animation duration. While functional for this prototype, a more robust solution would rely on animation events or a data-driven timing configuration to avoid dependence on normalized time assumptions.

This would improve maintainability and reduce the risk of desynchronization if animation clips change.

---

### Evolving Toward an Event-Driven Combat Flow

The current combat flow is primarily driven by an Update-based evaluation loop within the `CombatService`, where attack conditions are continuously checked.

While this approach keeps the logic straightforward and appropriate for a focused prototype, it could evolve toward clearer state- and event-driven boundaries — particularly around attack timing, target validity, and damage scheduling.

Instead of polling attack eligibility every frame, transitions such as `OnStateEntered(Attack)`, `OnTargetAcquired`, or `OnCooldownCompleted` could explicitly trigger combat actions.

This would:

- Reduce implicit per-frame checks  
- Clarify ownership of timing decisions  
- Improve extensibility for additional systems (combat effects, achievements, analytics hooks)  

Moving from a time-polled loop to explicit state/event transitions is not only a readability improvement — it is also a scalability decision that helps manage complexity before subtle timing or synchronization bugs appear.

---

## Next Steps

### 1. Complete the Full Combat Loop

The next priority would be to finalize the full combat loop by adding:

- Enemy attack behaviors  
- Player damage handling  
- Clear combat feedback (hit reactions, VFX/SFX, UI)

With a complete loop, proper tuning of pacing and difficulty would become possible.

---

### 2. Introduce ScriptableObject-Driven Enemy Archetypes

Enemy behaviors and stats could be made fully data-driven using ScriptableObjects. This would allow:

- Easier balancing  
- Faster iteration  
- Scalable addition of new enemy types  
- Reduced need to modify core gameplay logic  

---

## Comments

I intentionally prioritized:

- Clean separation of responsibilities  
- Readable and explainable systems  
- Stability over feature density  
- Respecting the 10-hour constraint  

The goal was to deliver a focused and well-structured prototype that demonstrates solid gameplay architecture rather than maximizing the number of implemented features.
