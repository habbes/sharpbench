# 2024-05-17 Anonymous sessions

Sharpbench is used without requiring authenticaton (for now). However, it would be good to retain a session about a specfic client since we do server-to-client communication in the form of real time log transmission. At the moment, the server broadcasts logs from each job to all connectied WebSocket clients, then the client filters out the logs for jobs it does not know about. This is definitely a bad idea. We need a way to correlate job ids to web socket connection.

Here's what I have in mind:

- a client generates a random session uuid (this can also be generated on the server to ensure it's really unique and the format is consistent)
- when the the client sends a job request to the server, it attaches the session ID
- when a client initiates a websocket connection with the server, it also attaches the session ID
- the server keeps track of which jobs belong to which session
- when logs arrive, the server only sends them to the webstocket connection associated with the session ID

The benefits of the session ID go beyond log transmission. We can persist the session ID in the browser cache. This way the client can continue to receive logs from their jobs even when they reload the browser. For this to be meaninfgul, we would also need to cache the job history client-side. The user can choose to clear their session to return to a blank state.
