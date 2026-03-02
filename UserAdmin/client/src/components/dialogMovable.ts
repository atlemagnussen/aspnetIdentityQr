export class DialogMovable {
    /**
     * adds dragging to dialog (and maybe popover)
     */
    constructor(dialog: HTMLElement, header?: HTMLDivElement) {
        
        this.dialog = dialog
        this.handle = header ?? dialog.querySelector("header") as HTMLDivElement
        this.setup()
    }

    dialog: HTMLElement
    handle: HTMLDivElement

    initialized = false
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
    
    initializeDrag(clientX: number, clientY: number) {
        this.isDragging = true
        
        this.offsetX = clientX - this.dialog.offsetLeft // offset from where on the draggable header you drag
        this.offsetY = clientY - this.dialog.offsetTop
        
        this.dialog.classList.add('is-dragging')
    }
    startDrag = (e: PointerEvent) => {
        if ((e.target as HTMLSpanElement).id == "btnClosePopover")
            return
        e.preventDefault()
        this.initializeDrag(e.clientX, e.clientY)
        this.handle.setPointerCapture(e.pointerId);
    }
    
    move(clientX: number, clientY: number) {
        if (!this.isDragging)
            return

        if (!this.hasBeenMoved) {
            const rectInit = this.dialog.getBoundingClientRect()
            this.dialog.style.top = `${rectInit.top}px`
            this.dialog.style.left = `${rectInit.left}px`
            this.offsetX = clientX - rectInit.left;
            this.offsetY = clientY - rectInit.top;
            this.dialog.classList.add("moved")
        }

        const newX = clientX - this.offsetX
        const newY = clientY - this.offsetY

        this.dialog.style.top = `${newY}px`
        this.dialog.style.left = `${newX}px`

        this.hasBeenMoved = true
    }
    dragging = (e: PointerEvent) => {
        e.preventDefault()
        this.move(e.clientX, e.clientY)
    }

    endDrag = (e: PointerEvent) => {
        if (this.isDragging) {
            this.isDragging = false
            this.dialog.classList.remove('is-dragging')
        }
        this.handle.releasePointerCapture(e.pointerId)
    }
}