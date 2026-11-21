import { LitElement } from "lit"
import { state } from "lit/decorators.js"

/**
 * Base for admin views: tracks busy/error/notice for async actions so every view
 * reports failures (server ProblemDetails messages) the same way.
 */
export class AdminElement extends LitElement {
    @state()
    protected busy = false

    @state()
    protected error?: Error

    @state()
    protected notice = ""

    /** Run an action; shows `success` on completion or the error message on failure. Returns the result or undefined. */
    protected async run<T>(action: () => Promise<T>, success = ""): Promise<T | undefined> {
        this.busy = true
        this.error = undefined
        this.notice = ""
        try {
            const result = await action()
            this.notice = success
            return result
        } catch (err: any) {
            this.error = err instanceof Error ? err : new Error(String(err))
            return undefined
        } finally {
            this.busy = false
        }
    }
}

/** Read the value from a wa-input / wa-select / wa-textarea change or input event */
export const valueOf = (e: Event) => (e.target as HTMLInputElement).value

/** Read the checked state from a wa-checkbox / wa-switch change event */
export const checkedOf = (e: Event) => (e.target as HTMLInputElement).checked

/*
 * wa-checkbox / wa-switch (Web Awesome 3.2.1): bind BOTH `?checked=${v} .checked=${v}`.
 * On first update they reset `checked` to `defaultChecked`, which comes from the `checked` attribute,
 * so a property-only binding renders unchecked. The property binding keeps later changes (revert) in sync.
 */
