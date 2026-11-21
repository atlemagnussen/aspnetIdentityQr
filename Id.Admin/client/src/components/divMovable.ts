export class DivMovable {

    /**
     * adds dragging to popover
     */
    constructor(div: HTMLElement, header?: HTMLDivElement) {
        this.div = div
        this.handle = header ?? div.querySelector("header") as HTMLDivElement
        this.setup()
    }

    div: HTMLElement
    handle: HTMLDivElement

    hasBeenMoved = false
    isDragging = false
    offsetX = 0
    offsetY = 0

    setup() {
        this.handle.addEventListener("pointerdown", this.startDrag)
        document.addEventListener("pointermove", this.dragging)
        document.addEventListener("pointerup", this.endDrag)
        document.addEventListener("pointercancel", this.endDrag)
    }
    teardown() {
        this.handle.removeEventListener("pointerdown", this.startDrag)
        document.removeEventListener("pointermove", this.dragging)
        document.removeEventListener("pointerup", this.endDrag)
        document.removeEventListener("pointercancel", this.endDrag)
    }

    initialize(clientX: number, clientY: number) {
        this.isDragging = true
        
        this.setOffset(clientX, clientY)

        this.div.classList.add('is-dragging')
    }
    setOffset(clientX: number, clientY: number) {
        this.offsetX = clientX
        this.offsetY = clientY
    }
    startDrag = (e: PointerEvent) => {
        if ((e.target as HTMLSpanElement).id == "btnClosePopover")
            return
        e.preventDefault()
        this.initialize(e.clientX, e.clientY)
        this.handle.setPointerCapture(e.pointerId);
    }
    
    /** move */
    move(clientX: number, clientY: number) {
        if (!this.isDragging)
            return

        let newX = clientX - this.offsetX
        let newY = clientY - this.offsetY

        this.div.style.marginLeft = `${newX}px`
        this.div.style.marginTop = `${newY}px`
        this.hasBeenMoved = true
    }
    dragging = (e: PointerEvent) => {
        e.preventDefault()
        this.move(e.clientX, e.clientY)
    }

    /** end */
    endDrag = (e: PointerEvent) => {
        if (this.isDragging) {
            this.isDragging = false
            this.div.classList.remove('is-dragging')
        }
        this.handle.releasePointerCapture(e.pointerId)
    }
}