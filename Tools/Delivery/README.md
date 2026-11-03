# SDK delivery review tools

These are internal file-inventory tools, outside Unity Assets. They are not runtime dependencies or customer Editor setup. Use Python 3.11 or newer; only the Python standard library is required.

The recorded inventory targets the local reviewed working tree, which includes unpublished feature assets. Run against that source snapshot or after synchronizing its feature changes. The tool deliberately fails if an expected sample/prefab is absent; publishing these tools and documents alone does not publish the runtime changes or validate the remote source tree.

Run from the repository root:

```sh
python Tools/Delivery/prepare_delivery_review.py
python Tools/Delivery/validate_delivery_review.py
```

The preparation tool reads Assets, Packages and ProjectSettings, writes the CSV inventory documents in Docs/Delivery and creates an ignored local review under Builds/SDKDelivery_20261103. It preserves metadata and hashes copied inputs. Dedicated local-only modules and internal validation files are omitted. Shared files that require publication review remain marked REVIEW_HOLD. This is a review copy, not a validated SDK export.

The review copy alone has its package manifest/lock filtered to remove internal Unity-MCP. The source project's packages, code, assets and settings remain unchanged. ProjectSettings in the copy are references; do not overwrite an application project's settings or reuse developer signing configuration.

Preparation refuses to overwrite an existing ReviewFiles directory. For another snapshot, use `--output Builds/SDKDelivery_NEW_LABEL`. The validation tool accepts the same `--output` option. Preserve previous evidence. Neither script deletes files, launches Unity/apps, communicates with a device, or changes the Git index/remotes.

Review the generated file/native/package hashes, Resources lookup list and cross-folder dependency edges. The GUID scan covers local serialized text references only; package assets, dynamic loads, native code and real Unity compilation need their own validation. Existing source script comments or disabled references can appear in lexical lookup results.
