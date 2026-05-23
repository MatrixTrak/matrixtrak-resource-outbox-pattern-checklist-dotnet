# Outbox pattern checklist + schema (.NET)

Production-focused companion repository for a MatrixTrak resource.

## What This Repository Is

This repository is the public distribution surface for the linked MatrixTrak resource.
It is designed for quick implementation support, community sharing, and stable versioned references.

## Canonical MatrixTrak Links

- Resource page (canonical): https://matrixtrak.com/resources/outbox-pattern-checklist-dotnet
- Primary blog posts:
  - https://matrixtrak.com/blog/outbox-pattern-without-enterprise-baggage-reliable-writes-events

## Resource Summary

A minimal schema, polling publisher template, and rollout checklist for reliable event publishing in .NET.

## Repository Contents

- `resources/` contains shipped files copied from MatrixTrak public ship assets when available
- `docs/post-mapping.md` maps this resource to related blog posts
- `docs/resource-files.md` lists included files and source mapping
- Included shipped files:
  - resources/outbox-publisher-template.cs
  - resources/outbox-rollout-checklist.md
  - resources/outbox-table-schema.sql
  - resources/README.md

## Who This Is For

- Engineers handling production incidents and reliability gaps
- Teams implementing or validating practical safeguards
- Readers coming from community channels who need canonical references

## Included Mapping

Primary mapping (post frontmatter resources):
- outbox-pattern-without-enterprise-baggage-reliable-writes-events - Outbox pattern: reliable writes + events without the enterprise baggage

Secondary mapping (resource relatedPosts):
- idempotency-keys-for-apis-prevent-duplicate-orders-emails-writes - Idempotency keys for APIs: stop duplicate orders, emails, and writes

## Usage Notes

- Treat MatrixTrak pages as the canonical long-form guidance.
- Use this repo for practical implementation support and sharing.
- For updates, always check the canonical resource page first.

## Attribution
Use MatrixTrak canonical links above for the full context and updates.
