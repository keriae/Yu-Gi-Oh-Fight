# Local prompt token budgeting

`o200k_base.tiktoken` is the OpenAI tiktoken merge-rank table from
https://openaipublic.blob.core.windows.net/encodings/o200k_base.tiktoken.
Its SHA-256 is `446a9538cb6c348e3516120d7c08b09f57c36495e2acfffe59a5bf8b0cfb1a2d`.
The source builder verifies and embeds this table in Assembly-CSharp, so the
game performs no tokenizer download and needs no external Python process.

The matching pre-tokenization expression and byte-pair count are implemented
in `PromptTokenCounter.cs`. See `LICENSE.tiktoken` for tiktoken's MIT license.
Local verification uses tiktoken 0.12.0
`get_encoding("o200k_base").encode_ordinary`, including Chinese, English,
JSON, Unicode, random mixtures and long repeated text. Test fixtures are
excluded from Git.

This is a local estimate rather than a promise about an arbitrary provider's
model tokenizer. Total request budgeting adds 5% and 1024 tokens of formatting
reserve, then reserves the merged `max_output_tokens` value.
