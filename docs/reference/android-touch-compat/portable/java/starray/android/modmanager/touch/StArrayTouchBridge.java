package starray.android.modmanager.touch;

import android.view.MotionEvent;

public final class StArrayTouchBridge {
    private StArrayTouchBridge() {
    }

    public static boolean forwardMotionEvent(MotionEvent event) {
        if (event == null) {
            return false;
        }

        int pointerIndex = event.getActionIndex();
        if (pointerIndex < 0 || pointerIndex >= event.getPointerCount()) {
            pointerIndex = 0;
        }

        int action = event.getActionMasked();
        int toolType = event.getToolType(pointerIndex);
        int buttonState = event.getButtonState();
        boolean consumed = false;
        if (action == MotionEvent.ACTION_MOVE) {
            for (int i = 0; i < event.getHistorySize(); ++i) {
                consumed |= nativeForwardMotionEvent(
                        action,
                        event.getHistoricalX(pointerIndex, i),
                        event.getHistoricalY(pointerIndex, i),
                        toolType,
                        buttonState);
            }
        }

        consumed |= nativeForwardMotionEvent(
                action,
                event.getX(pointerIndex),
                event.getY(pointerIndex),
                toolType,
                buttonState);
        return consumed;
    }

    public static void setOverlayVisible(boolean visible) {
        nativeSetOverlayVisible(visible);
    }

    public static void setModalInputCapture(boolean active) {
        nativeSetModalInputCapture(active);
    }

    public static boolean isModalInputCaptureActive() {
        return nativeIsModalInputCaptureActive() != 0;
    }

    public static void requestModalClose() {
        nativeRequestModalClose();
    }

    public static boolean consumeModalCloseRequest() {
        return nativeTakeModalCloseRequest() != 0;
    }

    private static native boolean nativeForwardMotionEvent(
            int action,
            float x,
            float y,
            int toolType,
            int buttonState);

    private static native void nativeSetOverlayVisible(boolean visible);
    private static native void nativeSetModalInputCapture(boolean active);
    private static native int nativeIsModalInputCaptureActive();
    private static native void nativeRequestModalClose();
    private static native int nativeTakeModalCloseRequest();
}
