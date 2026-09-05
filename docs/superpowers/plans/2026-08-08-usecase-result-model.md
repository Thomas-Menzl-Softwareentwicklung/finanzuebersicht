# UseCaseResult (#274) Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Introduce a lightweight `UseCaseResult` / `UseCaseResult<T>` with `UseCaseErrorCode` for Application Save-flows (Transaction, Transfer, Account), map codes to i18n in Presentation, document the convention.

**Architecture:** Application returns success/failure without throwing for expected domain errors. Presentation maps `UseCaseErrorCode` → `ResourceKeys`. Unexpected exceptions still logged + user alert. No OneOf NuGet.

**Tech stack:** .NET 10, xUnit, NSubstitute, existing ResourceKeys/resx.

## File map

| File | Role |
|------|------|
| `Application/Results/UseCaseErrorCode.cs` | Stable error codes |
| `Application/Results/UseCaseResult.cs` | Non-generic + generic Result |
| `Presentation/Services/UseCaseErrorPresenter.cs` | Code → localized string + dialog helper |
| Save* UseCases + ViewModels + tests | First adopters |
| `copilot-instructions.md` | Convention |

## Tasks

1. RED/GREEN: `UseCaseResult` unit tests + types
2. Migrate `SaveTransferUseCase` (+ tests)
3. Migrate `SaveTransactionDetailUseCase` (+ tests)
4. Migrate `SaveAccountDetailUseCase` (license via `CheckCreateLimit`, + tests)
5. `UseCaseErrorPresenter` + ResourceKeys/resx + three Detail ViewModels
6. Docs in `copilot-instructions.md`; verify test suite for touched projects
