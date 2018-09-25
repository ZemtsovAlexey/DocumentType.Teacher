import React, {Component} from 'react';
import { HubConnectionBuilder, LogLevel } from '@aspnet/signalr';
import { Col, Grid, Row, Thumbnail, ButtonGroup, Button } from 'react-bootstrap';
import { NetSettings } from './NetSettings';

export class DocumentTypeTeacher extends Component {
    displayName = DocumentTypeTeacher.name;

    constructor(props) {
        super(props);
        this.state = {
            hubConnection: null,
            file: null,
            iteration: 0,
            error: 0,
            successes: 0,
            computeResult: 0,
            imageSrc: '/thumbnail.png'
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

        this.setState({hubConnection}, () => {
            this.state.hubConnection
                .start()
                .then(() => console.log('Connection started!'))
                .catch(err => console.log('Error while establishing connection :('));

            this.state.hubConnection.on('IterationChange', (result) => {
                this.setState({ iteration: result.iteration, error: result.error, successes: result.successes });
            });
        });
    };

    onFormSubmit = async (e) => {
        e.preventDefault();

        this.fileUpload(this.state.file)
            .then(response => response.blob())
            .then(images => {
                let outside = URL.createObjectURL(images);
                this.setState({ imageSrc: outside });
                console.log(outside)
            });
    };
    
    onChange(e) {
        this.setState({file:e.target.files[0]})
    }

    fileUpload(file){
        let data = new FormData();
        data.append('file', file);
        
        return fetch('api/net/Compute/image', {
            method: 'POST',
            body: data
        });
    }

    async teachRun(){
        await fetch('api/net/teach/run', {
            method: 'POST'
        });
    }

    async teachStop(){
        await fetch('api/net/teach/stop', {
            method: 'POST'
        });
    }

    render() {
        return (
            <div>
                <h1>Teacher</h1>

                <Row>
                    <Col sm={7}>
                        <Thumbnail href="#" alt="171x180" src={this.state.imageSrc} />
                    </Col>
                    <Col sm={4}>
                        <NetSettings />
                    </Col>
                </Row>
                <Row>
                    <Col sm={3}>
                        <form onSubmit={this.onFormSubmit} style={{display: "inline"}}>
                            <h1>File Upload</h1>
                            <input type="file" onChange={this.onChange} />
                            <button type="submit">Upload</button>
                            <label>{this.state.computeResult}</label>
                        </form>
                    </Col>
                    <Col sm={9}></Col>
                </Row>
                <Row>
                    <Col sm={2}>
                        <ButtonGroup>
                            <Button onClick={this.teachRun}>teach run</Button>
                            <Button onClick={this.teachStop}>teach stop</Button>
                        </ButtonGroup>
                    </Col>
                    <Col sm={1}>
                    </Col>
                    <Col sm={1}>
                        <label>Iteration: </label>
                        <label>{this.state.iteration}</label>
                    </Col>
                    <Col sm={2}>
                        <label>Error: </label>
                        <label>{this.state.error}</label>
                    </Col>
                    <Col sm={2}>
                        <label>Successes: </label>
                        <label>{this.state.successes}</label>
                    </Col>
                </Row>
            </div>
        );
    }
}
