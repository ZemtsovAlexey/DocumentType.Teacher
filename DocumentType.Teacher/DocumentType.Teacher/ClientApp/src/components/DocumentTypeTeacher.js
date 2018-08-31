import React, {Component} from 'react';
import { HubConnectionBuilder, LogLevel } from '@aspnet/signalr';

export class DocumentTypeTeacher extends Component {
    displayName = DocumentTypeTeacher.name;

    constructor(props) {
        super(props);
        this.state = {
            nick: '',
            message: '',
            messages: [],
            hubConnection: null,
            file: null
        };

        this.onFormSubmit = this.onFormSubmit.bind(this);
        this.onChange = this.onChange.bind(this);
        this.fileUpload = this.fileUpload.bind(this);
    }

    componentDidMount = () => {
        const hubConnection = new HubConnectionBuilder()
            .withUrl(`https://${document.location.host}/teacherHub`)
            .configureLogging(LogLevel.Information)
            .build();

        const nick = window.prompt('Your name:', 'John');

        this.setState({hubConnection, nick}, () => {
            this.state.hubConnection
                .start()
                .then(() => console.log('Connection started!'))
                .catch(err => console.log('Error while establishing connection :('));

            this.state.hubConnection.on('sendToAll', (nick, receivedMessage) => {
                const text = `${nick}: ${receivedMessage}`;
                const messages = this.state.messages.concat([text]);
                this.setState({messages});
            });
        });
    };

    sendMessage = () => {
        this.state.hubConnection
            .invoke('SendToAll', this.state.nick, this.state.message)
            .catch(err => console.error(err));

        this.setState({message: ''});
    };

    onFormSubmit(e){
        e.preventDefault();

        const data = new FormData(e.target);
        
        this.fileUpload(data).then((response)=>{
            console.log(response.data);
        })
    }
    
    onChange(e) {
        this.setState({file:e.target.files[0]})
    }

    async fileUpload(file){
        const data = new FormData();
        data.append('file',file);

        const formData = {};
        for (const field in this.refs) {
            formData[field] = this.refs[field].value;
        }
        
        const request = 
        {
            method: 'POST',
            headers: {
                'content-type': 'application/x-www-form-urlencoded'
            },
            body: file
        };

        return await fetch('api/SampleData/UploadFile', request);
        
        // return post(url, formData,config)
    }


    render() {
        return (
            <div>
                <h1>Teacher</h1>

                <img width={200} height={200} style={{background: "silver", border: "1px solid black"}}/>
                <button onClick={this.incrementCounter}>Increment</button>
                <div>
                    <br />
                    <input
                        type="text"
                        value={this.state.message}
                        onChange={e => this.setState({ message: e.target.value })}
                    />

                    <button onClick={this.sendMessage}>Send</button>

                    <div>
                        {this.state.messages.map((message, index) => (
                            <span style={{display: 'block'}} key={index}> {message} </span>
                        ))}
                    </div>
                </div>
                <form onSubmit={this.onFormSubmit}>
                    <h1>File Upload</h1>
                    <input type="file" onChange={this.onChange} />
                    <button type="submit">Upload</button>
                </form>
            </div>
        );
    }
}
