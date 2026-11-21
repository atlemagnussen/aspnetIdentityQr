import { css } from "lit"

/** Shared styles for admin views (shadow DOM doesn't see the global site.css) */
export const adminStyles = css`
    :host {
        display: block;
        color: var(--wa-color-text-normal);
    }
    h2, h3, h4 {
        margin: 0;
    }
    /* keep tabs reachable while a long tab panel scrolls inside a dialog */
    wa-tab-group::part(nav) {
        position: sticky;
        top: calc(-1 * var(--wa-space-m));
        z-index: 2;
        background: var(--wa-color-surface-default);
    }
    .toolbar {
        display: flex;
        flex-wrap: wrap;
        align-items: end;
        gap: var(--wa-space-s);
        margin-block: var(--wa-space-s) var(--wa-space-m);
    }
    .toolbar .grow {
        flex: 1 1 16rem;
    }
    .header {
        display: flex;
        flex-wrap: wrap;
        justify-content: space-between;
        align-items: center;
        gap: var(--wa-space-s);
    }
    table {
        width: 100%;
        border-collapse: collapse;
    }
    th {
        text-align: left;
        font-weight: var(--wa-font-weight-semibold);
        font-size: var(--wa-font-size-s);
        color: var(--wa-color-text-quiet);
        padding: var(--wa-space-xs);
        border-bottom: var(--wa-border-width-s) solid var(--wa-color-surface-border);
    }
    td {
        padding: var(--wa-space-xs);
        border-bottom: var(--wa-border-width-s) solid var(--wa-color-surface-border);
        vertical-align: middle;
    }
    tbody tr:hover {
        background: var(--wa-color-surface-raised);
    }
    tr.clickable {
        cursor: pointer;
    }
    td.right, th.right {
        text-align: right;
    }
    .muted {
        color: var(--wa-color-text-quiet);
        font-size: var(--wa-font-size-s);
    }
    .mono {
        font-family: var(--wa-font-family-code);
        font-size: var(--wa-font-size-s);
        word-break: break-all;
    }
    .tags {
        display: flex;
        flex-wrap: wrap;
        gap: var(--wa-space-2xs);
    }
    .actions {
        display: flex;
        flex-wrap: wrap;
        justify-content: flex-end;
        gap: var(--wa-space-xs);
    }
    .stack {
        display: grid;
        gap: var(--wa-space-s);
        align-content: start;
    }
    .grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(15rem, 1fr));
        gap: var(--wa-space-s) var(--wa-space-m);
        align-items: start;
    }
    .section {
        display: grid;
        gap: var(--wa-space-s);
        padding-block: var(--wa-space-s);
    }
    .section + .section {
        border-top: var(--wa-border-width-s) solid var(--wa-color-surface-border);
    }
    .help {
        margin: 0;
        color: var(--wa-color-text-quiet);
        font-size: var(--wa-font-size-s);
    }
    .notice {
        color: var(--wa-color-success-on-quiet);
        background: var(--wa-color-success-fill-quiet);
        border-radius: var(--wa-border-radius-m);
        padding: var(--wa-space-xs) var(--wa-space-s);
        font-size: var(--wa-font-size-s);
    }
    .empty {
        padding: var(--wa-space-l);
        text-align: center;
        color: var(--wa-color-text-quiet);
    }
    wa-tab-panel::part(base) {
        padding-inline: 0;
    }
    @media (max-width: 720px) {
        .hide-sm {
            display: none;
        }
    }
`
