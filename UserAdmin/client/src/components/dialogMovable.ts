export class DialogMovable {

    /**
     * adds dragging to popover
     */
    constructor(dialog: HTMLElement, header?: HTMLDivElement) {
        
        this.dialog = dialog
        this.handle = header ?? dialog.querySelector("header") as HTMLDivElement
        this.setup()
    }

    dialog: HTMLElement
    handle: HTMLDivElement

    hasBeenMoved = false
    isDragging = false
    offsetX = 0
    offsetY = 0

    setup() {
        //this.popover.addEventListener("toggle", this.beforeToggle as EventListener)
        
        this.handle.addEventListener("pointerdown", this.startDrag)
        document.addEventListener("pointermove", this.dragging)
        document.addEventListener("pointerup", this.endDrag)
        document.addEventListener("pointercancel", this.endDrag)
    }
    teardown() {
        //this.popover.removeEventListener("toggle", this.beforeToggle as EventListener)

        this.handle.removeEventListener("pointerdown", this.startDrag)
        document.removeEventListener("pointermove", this.dragging)
        document.removeEventListener("pointerup", this.endDrag)
        document.removeEventListener("pointercancel", this.endDrag)
    }

    /** initialize */
    initialize(clientX: number, clientY: number) {
        this.isDragging = true
        
        this.offsetX = clientX - this.dialog.offsetLeft // offset from where on the draggable header you drag
        this.offsetY = clientY - this.dialog.offsetTop
        
        this.dialog.classList.add('is-dragging')
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

        const newX = clientX - this.offsetX
        const newY = clientY - this.offsetY

        this.dialog.style.marginLeft = `${newX}px`
        this.dialog.style.marginTop = `${newY}px`
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
            this.dialog.classList.remove('is-dragging')
        }
        this.handle.releasePointerCapture(e.pointerId)
    }
}