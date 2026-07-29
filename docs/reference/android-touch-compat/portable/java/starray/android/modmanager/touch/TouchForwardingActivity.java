package starray.android.modmanager.touch;

import android.view.KeyEvent;
import android.view.MotionEvent;

public class TouchForwardingActivity extends com.unity3d.player.UnityPlayerActivity {
    @Override
    public boolean dispatchTouchEvent(MotionEvent event) {
        if (StArrayTouchBridge.isModalInputCaptureActive()) {
            // Preserve the original Unity Canvas/IMGUI event path.
            return super.dispatchTouchEvent(event);
        }

        if (StArrayTouchBridge.forwardMotionEvent(event)) {
            return true;
        }

        // Optional observe-only or async gameplay producer belongs here.
        return super.dispatchTouchEvent(event);
    }

    @Override
    public boolean dispatchKeyEvent(KeyEvent event) {
        if (StArrayTouchBridge.isModalInputCaptureActive() &&
            event != null && event.getKeyCode() == KeyEvent.KEYCODE_BACK) {
            if (event.getAction() == KeyEvent.ACTION_UP) {
                StArrayTouchBridge.requestModalClose();
            }
            return true;
        }
        return super.dispatchKeyEvent(event);
    }
}
