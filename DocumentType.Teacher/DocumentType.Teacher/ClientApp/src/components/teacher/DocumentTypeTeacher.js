import React, {Component} from 'react';
import { HubConnectionBuilder, LogLevel } from '@aspnet/signalr';
import { Col, Grid, Row, Thumbnail, Table } from 'react-bootstrap';
import { NetSettings } from './NetSettings';

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
            .withUrl(`${document.location.protocol}//${document.location.host}/teacherHub`)
            .configureLogging(LogLevel.Information)
            .build();

        const nick = 'John';//window.prompt('Your name:', 'John');

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

        this.fileUpload(this.state.file).then((response)=>{
            console.log(response.data);
        })
    }
    
    onChange(e) {
        this.setState({file:e.target.files[0]})
    }

    async fileUpload(file){
        return await fetch('api/SampleData/UploadFile', {
            method: 'POST',
            body: new FormData().append('file', file)
        });
    }


    render() {
        return (
            <div>
                <h1>Teacher</h1>

                <Row>
                    <Col sm={3}>
                        <Thumbnail href="#" alt="171x180" src="/thumbnail.png" />
                    </Col>
                    <Col sm={9}>
                        <NetSettings />
                    </Col>
                </Row>
                <Row>
                    <Col sm={3}>
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
                                    <span style={{ display: 'block' }} key={index}> {message} </span>
                                ))}
                            </div>
                        </div>
                        <form onSubmit={this.onFormSubmit}>
                            <h1>File Upload</h1>
                            <input type="file" onChange={this.onChange} />
                            <button type="submit">Upload</button>
                        </form>
                    </Col>
                    <Col sm={9}></Col>
                </Row>
            </div>
        );
    }
}
